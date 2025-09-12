using TicketProcessor.Domain;

namespace TicketProcessor.Application.Interfaces;

public interface IEventService
{
    Task<EventDto> CreateEventAsync(CreateEventDto request, CancellationToken ct = default);
    Task<PagedResult<EventListItemDto>> GetEventsListAsync(PublicEventsQuery query, CancellationToken ct = default);
    Task<CreateReservationResultDto> CreateReservationAsync(CreateReservationRequestDto request, CancellationToken ct = default);
    Task<PurchaseResultDto> PurchaseAsync(PurchaseRequestDto request, CancellationToken ct = default);
    Task<EventTicketTypeDto> UpsertTicketAsync(EventTicketTypeDto input, CancellationToken ct = default);
    Task<EventDto> UpdateEventAsync(EventDto input, CancellationToken ct = default);
    Task DeleteReservationAsync(Guid reservationId, CancellationToken ct = default);
    Task DeleteEventAsync(Guid eventId, CancellationToken ct = default);
    Task DeleteTicketAsync(Guid eventTicketTypeId, CancellationToken ct = default);
}