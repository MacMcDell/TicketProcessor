using AutoMapper;
using Microsoft.Extensions.Logging;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;

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

    /// <summary>
    /// sets up a new venue if needed.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal async Task<Guid> GetVenueIdFromRequest(CreateEventDto request, CancellationToken ct = default)
    {
        Guid venueId;
        if (request.VenueId is Guid vid)
        {
            if (!await _venues.ExistsAsync(vid, ct))
                throw new InvalidOperationException($"Venue {vid} not found.");
            venueId = vid;
        }
        else
        {
            var venueDto = new VenueDto
            {
                Id = Guid.NewGuid(),
                Name = request.VenueName!.Trim(),
                Capacity = request.VenueCapacity!.Value
            };
            venueId = await _venues.AddAsync(venueDto, ct);
        }
        return venueId;
    }

    public async Task<EventDto> CreateEventAsync(CreateEventDto request, CancellationToken ct = default)
    {
        
        var venueId = await GetVenueIdFromRequest(request, ct);
        
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
            throw new InvalidOperationException($"Duplicate ticket type '{dup.Key}' in ");

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

        var existingEvent = await _events.GetByIdAsync(input.Id!.Value, ct);
        if (existingEvent == null)
            throw new InvalidOperationException($"Event {input.Id} not found.");

        if (input.VenueId != existingEvent.VenueId)
        {
            if (!await _venues.ExistsAsync(input.VenueId, ct))
                throw new InvalidOperationException($"Venue {input.VenueId} not found for update.");
            existingEvent.VenueId = input.VenueId; // Update VenueId if valid and changed
        }
        
        existingEvent.Title = input.Title!.Trim();
        existingEvent.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        existingEvent.StartsAt = input.StartsAt;
        
        await _events.UpdateAsync(existingEvent, ct);
        await _uow.SaveChangesAsync(ct);

        return existingEvent;

        
    }

    /// <summary>
    /// soft delete reservation
    /// rolls back the sold tickets if the reservation was confirmed.
    /// </summary>
    /// <param name="reservationId"></param>
    /// <param name="ct"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task DeleteReservationAsync(Guid reservationId, CancellationToken ct)
    {
        var reservationDto = await _reservations.GetByIdAsync(reservationId, ct);
        if (reservationDto is null)
            throw new InvalidOperationException($"Reservation {reservationId} not found.");
        
        var eventTicketTypeDto = await _eventTicketTypes.GetByIdAsync(reservationDto.EventTicketTypeId, ct);
        if(eventTicketTypeDto is null)
            throw new InvalidOperationException($"Event ticket type {reservationDto.EventTicketTypeId} not found.");
        
        await _reservations.DeleteAsync(reservationId, ct);
       
        if (reservationDto.Status == ReservationStatus.Confirmed)
        {
            //rollback the sold tickets.
            await _eventTicketTypes.AdjustIncrementSold(reservationDto.EventTicketTypeId, -reservationDto.Quantity, ct);
        }
        
        await _uow.SaveChangesAsync(ct);
        
    }

    public async Task DeleteEventAsync(Guid eventId, CancellationToken ct = default)
    {
        await _events.DeleteAsync(eventId, ct);
        await _uow.SaveChangesAsync(ct);    
    }

    public async Task DeleteTicketAsync(Guid eventTicketTypeId, CancellationToken ct = default)
    {
        await _eventTicketTypes.DeleteAsync(eventTicketTypeId, ct);
        await _uow.SaveChangesAsync(ct);    
    }

    public async Task<PagedResult<EventListItemDto>> GetEventsListAsync(PublicEventsQuery query, CancellationToken ct = default)
        => await _events.GetEventsAsync(query, ct);

    public async Task<CreateReservationResultDto> CreateReservationAsync(CreateReservationRequestDto request, CancellationToken ct = default)
    {
         if (request.Quantity <= 0) 
             throw new InvalidOperationException("Quantity must be greater than 0.");
        
         if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) 
             throw new InvalidOperationException("IdempotencyKey is required.");

        // fetch ETT
        var eventTicketTypeDto = await _eventTicketTypes.GetByIdAsync(request.EventTicketTypeId, ct)
                  ?? throw new InvalidOperationException("Event ticket type not found.");

        // availability = capacity - sold - active pending holds
        var now = DateTimeOffset.UtcNow;
        var activePendingQty = await _reservations.CountActivePendingAsync(eventTicketTypeDto.Id!.Value, now, ct);
        var available = eventTicketTypeDto.Capacity - eventTicketTypeDto.Sold - activePendingQty;
        if (available < request.Quantity)
            throw new InvalidOperationException("Not enough tickets available.");

        // Try idempotency
        // set it on the redis layer
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
                    return new CreateReservationResultDto(
                        existingRes.Id, existingRes.EventTicketTypeId, existingRes.Quantity,
                        existingRes.ExpiresAt, existingRes.Status);
                }
            }
            // Otherwise, generic duplicate
            throw new InvalidOperationException("Duplicate idempotency key.");
        }

        // Create reservation row (Pending)
        var expires = now.Add(ttl);
        var dto = new ReservationResponseDto{
            Id = reservationId,
            EventTicketTypeId = eventTicketTypeDto.Id!.Value,
            Quantity = request.Quantity,
            Status = ReservationStatus.Pending,
            ExpiresAt = expires,
            IdempotencyKey = request.IdempotencyKey
        };

        await _reservations.AddAsync(dto, ct);
        await _uow.SaveChangesAsync(ct);

        return new CreateReservationResultDto(reservationId, eventTicketTypeDto.Id!.Value, request.Quantity, expires, ReservationStatus.Pending);
    }

    public async Task<PurchaseResultDto> PurchaseAsync(PurchaseRequestDto request, CancellationToken ct = default)
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
        
        //this is a fake payment processor.You would charge the card firt, get the token, then pass it to complete the purchase.
        var payload = new PaymentProcessorRequestDto( total, request.Currency, desc, request.PaymentToken, res.IdempotencyKey);
        await _payments.ChargeAsync(payload, ct);

        // 4) If payment succeeds, atomically finalize: increment Sold and confirm reservation
        // We rely on DB tx + concurrency at the repo level (IncrementSoldAsync updates Sold)
        await _eventTicketTypes.AdjustIncrementSold(res.EventTicketTypeId, res.Quantity, ct);

        var confirmed = res with
        {
            Status = ReservationStatus.Confirmed,
            ExpiresAt = now // collapse hold
        };
        await _reservations.UpdateAsync(confirmed, ct);
        await _uow.SaveChangesAsync(ct);

        return new PurchaseResultDto(
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
