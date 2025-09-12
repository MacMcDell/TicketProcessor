using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;
using TicketProcessor.Domain.Dto;
using TicketProcessor.Domain.Requests;

namespace TicketProcessor.Infrastructure.Repositories;

public sealed class VenueRepository : IVenueRepository
{
    private readonly TicketingDbContext _db;
    private readonly IMapper _mapper;
    public VenueRepository(TicketingDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        => _db.Venues.AnyAsync(v => v.Id == id, ct);

    public async Task<VenueDto?> GetAsync(Guid id, CancellationToken ct)
        => await _db.Venues
            .Where(v => v.Id == id)
            .ProjectTo<VenueDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

    public async Task<Guid> AddAsync(VenueDto venue, CancellationToken ct)
    {
        var entity = _mapper.Map<Venue>(venue); // map DTO -> EF entity
        entity.Id = venue.Id == Guid.Empty ? Guid.NewGuid() : venue.Id;
        await _db.Venues.AddAsync(entity, ct);
        return entity.Id;
    }

    public async Task UpdateAsync(VenueDto existingVenue, CancellationToken ct)
    {
        var entity = await _db.Venues.FirstOrDefaultAsync(x => x.Id == existingVenue.Id, ct)
                     ?? throw new InvalidOperationException("Event not found.");
        
        entity.Capacity = existingVenue.Capacity;
        entity.Name = existingVenue.Name;
        _db.Attach(entity);
        _db.Entry(entity).Property(x => x.Capacity).IsModified = true;
        _db.Entry(entity).Property(x => x.Name).IsModified = true;
    }

    public async Task<bool> FindByNameAsync(string requestName, CancellationToken ct)
    {
       var result = await _db.Venues.AnyAsync(v => v.Name == requestName, ct);
       return result;
    }
}

public sealed class EventRepository : IEventRepository
{
    private readonly TicketingDbContext _db;
    private readonly IMapper _mapper;
    public EventRepository(TicketingDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<Guid> AddAsync(EventDto evt, CancellationToken ct)
    {
        var entity = _mapper.Map<Event>(evt);
        entity.Id = evt.Id ?? Guid.NewGuid();
        await _db.Events.AddAsync(entity, ct);
        return entity.Id;
    }

    public async Task<Request.PagedResult<Request.EventListItemDto>> GetEventsAsync(Request.PublicEventsQuery q, CancellationToken ct)
    {
        var from = q.From ?? DateTimeOffset.UtcNow;
        var to = q.To;

        var baseQuery = _db.Events.AsNoTracking()
            .Join(_db.Venues.AsNoTracking(),
                e => e.VenueId,
                v => v.Id,
                (e, v) => new { e, v })
            .Where(x => x.e.StartsAt >= from && (to == null || x.e.StartsAt <= to));
        //overkill at the moment, but we can add more filters later on
            // .Where(x => q.VenueId == null || x.e.VenueId == q.VenueId)
            // .Where(x => string.IsNullOrWhiteSpace(q.Search) || EF.Functions.ILike(x.e.Title, $"%{q.Search}%"));

        var total = await baseQuery.CountAsync(ct); //get the total items before paging.

        var page = q.Page <= 0 ? 1 : q.Page;
        var size = q.PageSize <= 0 ? 20 : Math.Min(q.PageSize, 100);
        var skip = (page - 1) * size;

//todo refactor for pending reservations.. available should also include pending. 
        var items = await baseQuery
            .OrderBy(x => x.e.StartsAt)
            .Skip(skip)
            .Take(size)
            .Select(x => new Request.EventListItemDto(
                x.e.Id,
                x.e.Title,
                x.e.StartsAt,
                x.v.Id,
                x.v.Name,
                (from ett in _db.EventTicketTypes.AsNoTracking()
                    where ett.EventId == x.e.Id
                    orderby ett.Price
                    select new Request.TicketTypeAvailabilityDto(
                        ett.Id,
                        ett.Name,
                        ett.Price,
                        ett.Capacity,
                        ett.Sold,
                        Math.Max(0, ett.Capacity - ett.Sold)
                    )).ToList()
            ))
            .ToListAsync(ct);

        return new Request.PagedResult<Request.EventListItemDto>(items, total);
    }

    public async Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct) =>  await _db.Events.AsNoTracking()
        .Where(r => r.Id == id)
        .Select(r => new EventDto{ Id = r.Id, Title = r.Title, StartsAt = r.StartsAt, VenueId = r.VenueId })
        .FirstOrDefaultAsync(ct);

    public async Task UpdateAsync(EventDto existingEvent, CancellationToken ct)
    {
        var entity = await _db.Events.FirstOrDefaultAsync(x => x.Id == existingEvent.Id, ct)
                ?? throw new InvalidOperationException("Event not found.");
        
        entity.Id = existingEvent.Id!.Value;
        entity.Title = existingEvent.Title;
        entity.StartsAt = existingEvent.StartsAt;
        entity.VenueId = existingEvent.VenueId;
        _db.Attach(entity);
        _db.Entry(entity).Property(x => x.Title).IsModified = true;
        _db.Entry(entity).Property(x => x.StartsAt).IsModified = true;
    }
}

public sealed class ReservationRepository : IReservationRepository
{
    private readonly TicketingDbContext _db;
    public ReservationRepository(TicketingDbContext db) => _db = db;

    public async Task AddAsync(ReservationDto reservation, CancellationToken ct)
    {
        var entity = new Reservation
        {
            Id = reservation.Id,
            EventTicketTypeId = reservation.EventTicketTypeId,
            Quantity = reservation.Quantity,
            Status = reservation.Status,
            ExpiresAt = reservation.ExpiresAt,
            IdempotencyKey = reservation.IdempotencyKey
        };
        await _db.Reservations.AddAsync(entity, ct);
    }

    public Task<ReservationDto?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Reservations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReservationDto(r.Id, r.EventTicketTypeId, r.Quantity, r.Status, r.ExpiresAt, r.IdempotencyKey))
            .FirstOrDefaultAsync(ct);

    public Task<int> CountActivePendingAsync(Guid eventTicketTypeId, DateTimeOffset now, CancellationToken ct)
        => _db.Reservations.AsNoTracking()
            .Where(r => r.EventTicketTypeId == eventTicketTypeId && r.Status == ReservationStatus.Pending && r.ExpiresAt > now)
            .SumAsync(r => r.Quantity, ct);

    public async Task<ReservationDto?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
    {
        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == id, ct);
        return r is null
            ? null
            : new ReservationDto(r.Id, r.EventTicketTypeId, r.Quantity, r.Status, r.ExpiresAt, r.IdempotencyKey);
    }
    public async Task UpdateAsync(ReservationDto reservation, CancellationToken ct)
    {
        // Use the tracked entity if present; otherwise load and track it
        var e = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservation.Id, ct)
                ?? throw new InvalidOperationException("Reservation not found.");
        
        e.Id = reservation.Id;
        e.EventTicketTypeId = reservation.EventTicketTypeId;
        e.Quantity = reservation.Quantity;
        e.Status = reservation.Status;
        e.ExpiresAt = reservation.ExpiresAt;
        e.IdempotencyKey = reservation.IdempotencyKey;
        
        _db.Attach(e);
        _db.Entry(e).Property(x => x.Status).IsModified = true;
        _db.Entry(e).Property(x => x.ExpiresAt).IsModified = true;
       
    }
}

public sealed class EventTicketTypeRepository : IEventTicketTypeRepository
{
    private readonly TicketingDbContext _db;
    private readonly IMapper _mapper;
    public EventTicketTypeRepository(TicketingDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public Task AddRangeAsync(IEnumerable<EventTicketTypeDto> items, CancellationToken ct)
    {
        var entities = items.Select(i => new EventTicketType {
            Id = i.Id ?? Guid.NewGuid(),
            EventId = i.EventId,
            Name = i.Name,
            Price = i.Price,
            Capacity = i.Capacity,
            Sold = i.Sold
        }).ToList();

        _db.EventTicketTypes.AddRange(entities);
        return Task.CompletedTask;
    }

    public Task<EventTicketTypeDto?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.EventTicketTypes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new EventTicketTypeDto{Id = x.Id, EventId = x.EventId,Capacity = x.Capacity,Sold = x.Sold,Name = x.Name,Price = x.Price})
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsByNameAsync(Guid eventId, string name, CancellationToken ct)
        => _db.EventTicketTypes.AnyAsync(x => x.EventId == eventId && x.Name == name, ct);
    
    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        => _db.EventTicketTypes.AnyAsync(t => t.Id == id, ct);

    public async Task<Guid> AddAsync(EventTicketTypeDto ticketType, CancellationToken ct)
    {
        EventTicketType? entity = null;
        if (ticketType.Id == null || ticketType.Id == Guid.Empty)
        {
            if(ticketType.EventId == Guid.Empty)
                throw new InvalidOperationException("Event ID is required.");
            // New record: create and add
            entity = _mapper.Map<EventTicketType>(ticketType);
            entity.Id = Guid.NewGuid();
            await _db.EventTicketTypes.AddAsync(entity, ct);
        }
        else
        {
            // Existing record: retrieve and update
            entity = await _db.EventTicketTypes.FirstOrDefaultAsync(x => x.Id == ticketType.Id, ct);
            if (entity == null)
            {
                throw new InvalidOperationException($"EventTicketType with Id {ticketType.Id} not found for update.");
            }
            _mapper.Map(ticketType, entity); 
            _db.EventTicketTypes.Update(entity); 
        }
        
        return entity.Id;

    }

    public async Task IncrementSoldAsync(Guid eventTicketTypeId, int qty, CancellationToken ct)
    {
        // Let DbContext save in the surrounding UoW transaction.
        // DbUpdateConcurrencyException will be thrown by SaveChanges if xmin changed.
        // Track + update; xmin shadow column protects from stale updates
        var eventTicket = await _db.EventTicketTypes.FirstOrDefaultAsync(x => x.Id == eventTicketTypeId, ct)
                ?? throw new InvalidOperationException("Event ticket type not found.");

        // Optional guard: ensure non-negative and within capacity
        if (eventTicket.Sold + qty > eventTicket.Capacity)
            throw new InvalidOperationException("Capacity exceeded.");

        eventTicket.Sold += qty;

    }
}

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly TicketingDbContext _db;
    public UnitOfWork(TicketingDbContext db) => _db = db;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
