using TicketProcessor.Domain;

namespace TicketProcessor.Application.Interfaces;

public interface IEventTicketTypeRepository
{
    Task AddRangeAsync(IEnumerable<EventTicketTypeDto> items, CancellationToken ct);
    Task<EventTicketTypeDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(Guid eventId, string name, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task<Guid> AddAsync(EventTicketTypeDto ticketType, CancellationToken ct);
    Task AdjustIncrementSold(Guid eventTicketTypeId, int qty, CancellationToken ct);
    Task DeleteAsync(Guid eventTicketTypeId, CancellationToken ct);
    Task<PagedResult<EventTicketTypeDto>> GetAllTickets(PageQuery query, CancellationToken ct);
}