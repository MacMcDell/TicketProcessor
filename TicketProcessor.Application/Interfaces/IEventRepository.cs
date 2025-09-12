
using TicketProcessor.Domain.Dto;
using TicketProcessor.Domain.Requests;

namespace TicketProcessor.Application.Interfaces;

public interface IEventRepository
{
    Task<Guid> AddAsync(EventDto evt, CancellationToken ct);
    Task<Request.PagedResult<Request.EventListItemDto>> GetEventsAsync(Request.PublicEventsQuery query, CancellationToken ct);
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task UpdateAsync(EventDto existingEvent, CancellationToken ct);
}