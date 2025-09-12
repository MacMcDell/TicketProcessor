using TicketProcessor.Domain.Dto;
using TicketProcessor.Domain.Requests;

namespace TicketProcessor.Application.Interfaces;

public interface IEventService
{
    Task<EventDto> CreateEventAsync(Request.CreateEventDto request, CancellationToken ct = default);
    Task<Request.PagedResult<Request.EventListItemDto>> GetEventsListAsync(Request.PublicEventsQuery query, CancellationToken ct = default);
    Task<Request.CreateReservationResultDto> CreateReservationAsync(Request.CreateReservationRequestDto request, CancellationToken ct = default);
    Task<Request.PurchaseResultDto> PurchaseAsync(Request.PurchaseRequestDto request, CancellationToken ct = default);
    Task<EventTicketTypeDto> UpsertTicketAsync(EventTicketTypeDto input, CancellationToken ct);
    Task<EventDto> UpdateEventAsync(EventDto input, CancellationToken ct);
}