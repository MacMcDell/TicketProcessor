using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;
using TicketProcessor.Domain.Dto;

namespace TicketProcessor.Infrastructure.Repositories;

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