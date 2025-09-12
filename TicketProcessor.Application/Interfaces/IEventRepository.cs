
using TicketProcessor.Domain;

namespace TicketProcessor.Application.Interfaces;

public interface IEventRepository
{
    Task<Guid> AddAsync(EventDto evt, CancellationToken ct);
    Task<PagedResult<EventListItemDto>> GetEventsAsync(PublicEventsQuery query, CancellationToken ct);
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task UpdateAsync(EventDto existingEvent, CancellationToken ct);
    Task DeleteAsync(Guid eventId, CancellationToken ct);
}