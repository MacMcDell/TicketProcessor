using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;
using TicketProcessor.Domain.Dto;
using TicketProcessor.Domain.Requests;

namespace TicketProcessor.Infrastructure.Repositories;

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