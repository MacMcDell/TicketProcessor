using AutoMapper;
using Microsoft.Extensions.Logging;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;
using TicketProcessor.Domain.Dto;
using TicketProcessor.Domain.Requests;

namespace TicketProcessor.Application.Services;

public sealed class EventService : IEventService
{
    private readonly IVenueRepository _venues;
    private readonly IEventRepository _events;
    private readonly IEventTicketTypeRepository _eventTicketTypes;
    private readonly IReservationRepository _reservations;
    private readonly IIdempotencyService _idem;
    private readonly IUnitOfWork _uow;
    private readonly IPaymentGateway _payments;
    private readonly ILogger<EventService> _logger;
    private readonly IMapper _mapper;

    public EventService(
        IVenueRepository venues,
        IEventRepository eventsRepo,
        IEventTicketTypeRepository eventTicketTypes,
        IUnitOfWork uow,
        IMapper mapper, IReservationRepository reservations, IIdempotencyService idem, IPaymentGateway payments, ILogger<EventService> logger)
    {
        _venues = venues;
        _events = eventsRepo;
        _eventTicketTypes = eventTicketTypes;
        _uow = uow;
        _mapper = mapper;
        _reservations = reservations;
        _idem = idem;
        _payments = payments;
        _logger = logger;
    }

    public async Task<EventDto> CreateEventAsync(Request.CreateEventDto request, CancellationToken ct = default)
    {
        // 1) Venue
        Guid venueId;
        if (request.VenueId is Guid vid)
        {
            if (!await _venues.ExistsAsync(vid, ct))
                throw new InvalidOperationException($"Venue {vid} not found.");
            venueId = vid;
        }
        else
        {
            var venueDto = new VenueDto(Guid.NewGuid(), request.VenueName!.Trim(), request.VenueCapacity!.Value);
            venueId = await _venues.AddAsync(venueDto, ct);
        }

        // 2) Event
        var evtId = Guid.NewGuid();
        var evtDto = new EventDto{
            Id= evtId,
            VenueId= venueId,
            StartsAt= request.StartsAt,
            Title= request.Title.Trim(),
            Description= string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };
        await _events.AddAsync(evtDto, ct);

        // 3) Validate ticket names are unique within the request (case-insensitive)
        var dup = request.TicketTypes
            .GroupBy(t => t.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (dup != null)
            throw new InvalidOperationException($"Duplicate ticket type '{dup.Key}' in request.");

        // 4) Create EventTicketType rows (per-event)
        var ettDtos = request.TicketTypes.Select(t => new EventTicketTypeDto{
            Id = Guid.NewGuid(),
            EventId = evtId,
            Capacity = t.Capacity,
            Sold = 0,
            Name =t.Name?.Trim() ?? string.Empty,
            Price = t.Price
        }).ToList();

        await _eventTicketTypes.AddRangeAsync(ettDtos, ct);
        await _uow.SaveChangesAsync(ct);
    
       return evtDto;
    }
    
    public async Task<EventDto> UpdateEventAsync(EventDto input, CancellationToken ct)
    {
        _logger.LogInformation("Updating Event {method}", nameof(UpdateEventAsync));

        if (input.Id == Guid.Empty)
            throw new InvalidOperationException("Event ID is required for update.");

        // 1) Retrieve the existing event
        var existingEvent = await _events.GetByIdAsync(input.Id!.Value, ct);
        if (existingEvent == null)
            throw new InvalidOperationException($"Event {input.Id} not found.");

        // 2) Handle Venue update/validation if VenueId is changed and valid
        if (input.VenueId != existingEvent.VenueId)
        {
            if (!await _venues.ExistsAsync(input.VenueId, ct))
                throw new InvalidOperationException($"Venue {input.VenueId} not found for update.");
            existingEvent.VenueId = input.VenueId; // Update VenueId if valid and changed
        }

        // 3) Update event properties from input DTO
        existingEvent.Title = input.Title!.Trim();
        existingEvent.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        existingEvent.StartsAt = input.StartsAt;

        // 4) Persist changes
        await _events.UpdateAsync(existingEvent, ct);

        // 5) Save changes to Unit of Work
        await _uow.SaveChangesAsync(ct);

        return existingEvent;

        
    }

    public async Task DeleteReservationAsync(Guid reservationId, CancellationToken ct)
    {
        var res = await _reservations.GetByIdAsync(reservationId, ct);
        if (res is null)
            throw new InvalidOperationException($"Reservation {reservationId} not found.");
        
        var ett = await _eventTicketTypes.GetByIdAsync(res.EventTicketTypeId, ct);
        if(ett is null)
            throw new InvalidOperationException($"Event ticket type {res.EventTicketTypeId} not found.");
        
        if (res.Status != ReservationStatus.Confirmed)
        {
            await _reservations.DeleteAsync(reservationId, ct);
            //todo update ett.available
        }

        if (res.Status == ReservationStatus.Confirmed)
        {
            //rollback the sold tickets.
            await _eventTicketTypes.IncrementSoldAsync(res.EventTicketTypeId, -res.Quantity, ct);
            await _reservations.DeleteAsync(reservationId, ct);
        }
        
        await _uow.SaveChangesAsync(ct);
        
    }

    public async Task<Request.PagedResult<Request.EventListItemDto>> GetEventsListAsync(Request.PublicEventsQuery query, CancellationToken ct = default)
        => await _events.GetEventsAsync(query, ct);

    public async Task<Request.CreateReservationResultDto> CreateReservationAsync(Request.CreateReservationRequestDto request, CancellationToken ct = default)
    {
         if (request.Quantity <= 0) throw new InvalidOperationException("Quantity must be greater than 0.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) throw new InvalidOperationException("IdempotencyKey is required.");

        // fetch ETT
        var ett = await _eventTicketTypes.GetByIdAsync(request.EventTicketTypeId, ct)
                  ?? throw new InvalidOperationException("Event ticket type not found.");

        // availability = capacity - sold - active pending holds
        var now = DateTimeOffset.UtcNow;
        var activePendingQty = await _reservations.CountActivePendingAsync(ett.Id!.Value, now, ct);
        var available = ett.Capacity - ett.Sold - activePendingQty;
        if (available < request.Quantity)
            throw new InvalidOperationException("Not enough tickets available.");

        // Try idempotency
        var reservationId = Guid.NewGuid();
        var ttl = TimeSpan.FromSeconds(Math.Max(10, request.HoldSeconds)); // minimum 10s safety
        var (set, existing) = await _idem.TrySetAsync(request.IdempotencyKey, reservationId, ttl, ct);

        if (!set)
        {
            // Return existing reservation if known
            if (existing is Guid rid)
            {
                var existingRes = await _reservations.GetByIdAsync(rid, ct);
                if (existingRes is not null)
                {
                    return new Request.CreateReservationResultDto(
                        existingRes.Id, existingRes.EventTicketTypeId, existingRes.Quantity,
                        existingRes.ExpiresAt, existingRes.Status);
                }
            }
            // Otherwise, generic duplicate
            throw new InvalidOperationException("Duplicate idempotency key.");
        }

        // Create reservation row (Pending)
        var expires = now.Add(ttl);
        var dto = new ReservationDto{
            Id = reservationId,
            EventTicketTypeId = ett.Id!.Value,
            Quantity = request.Quantity,
            Status = ReservationStatus.Pending,
            ExpiresAt = expires,
            IdempotencyKey = request.IdempotencyKey
        };

        await _reservations.AddAsync(dto, ct);
        await _uow.SaveChangesAsync(ct);

        return new Request.CreateReservationResultDto(reservationId, ett.Id!.Value, request.Quantity, expires, ReservationStatus.Pending);
    }

    public async Task<Request.PurchaseResultDto> PurchaseAsync(Request.PurchaseRequestDto request, CancellationToken ct = default)
    {
        // 1) Load reservation FOR UPDATE (tracked) and validate
        var res = await _reservations.GetByIdForUpdateAsync(request.ReservationId, ct)
                  ?? throw new InvalidOperationException("Reservation not found.");

        if (res.Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Reservation is not pending.");

        var now = DateTimeOffset.UtcNow;
        if (res.ExpiresAt <= now)
        {
            res.Status = ReservationStatus.Expired;
            await _reservations.UpdateAsync(res, ct);
            await _uow.SaveChangesAsync(ct);
            throw new InvalidOperationException("Reservation has expired.");
        }

        // 2) Load EventTicketType to compute price
        var ett = await _eventTicketTypes.GetByIdAsync(res.EventTicketTypeId, ct)
                  ?? throw new InvalidOperationException("Event ticket type not found.");

        var unitPrice = ett is { } ? ett.Price : throw new InvalidOperationException("Invalid ticket type price.");
        var total = unitPrice * res.Quantity;

        // 3) Call payment gateway (external)
        var desc = $"Reservation {res.Id} for ETT {res.EventTicketTypeId} x{res.Quantity}";
        var payload = new Request.PaymentProcessorRequestDto( total, request.Currency, desc, request.PaymentToken, res.IdempotencyKey);
        await _payments.ChargeAsync(payload, ct);

        // 4) If payment succeeds, atomically finalize: increment Sold and confirm reservation
        // We rely on DB tx + concurrency at the repo level (IncrementSoldAsync updates Sold)
        await _eventTicketTypes.IncrementSoldAsync(res.EventTicketTypeId, res.Quantity, ct);

        var confirmed = res with
        {
            Status = ReservationStatus.Confirmed,
            ExpiresAt = now // collapse hold
        };
        await _reservations.UpdateAsync(confirmed, ct);
        await _uow.SaveChangesAsync(ct);

        return new Request.PurchaseResultDto(
            ReservationId: res.Id,
            EventTicketTypeId: res.EventTicketTypeId,
            Quantity: res.Quantity,
            UnitPrice: unitPrice,
            TotalAmount: total,
            PurchasedAt: now
        );
    }

    public async Task<EventTicketTypeDto> UpsertTicketAsync(EventTicketTypeDto input, CancellationToken ct)
    {
        _logger.LogInformation("Upserting ticket {method}", nameof(UpsertTicketAsync));
        
        var dto = _mapper.Map<EventTicketTypeDto>(input);
        var id = await _eventTicketTypes.AddAsync(dto, ct);
        await _uow.SaveChangesAsync(ct);
        return dto with { Id = id };
        
        
    }


}
