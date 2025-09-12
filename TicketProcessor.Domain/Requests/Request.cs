namespace TicketProcessor.Domain.Requests
{
    public class Request
    {
        public record CreateEventDto(
            Guid? VenueId,
            string? VenueName,
            int? VenueCapacity,
            DateTimeOffset StartsAt,
            string Title,
            string? Description,
            IReadOnlyList<CreateEventTicketTypeDto> TicketTypes);

        public record CreateEventTicketTypeDto(
            string Name,
            decimal Price,
            int Capacity);

        public record CreateVenueDto(string Name, int Capacity);

        public sealed record TicketTypeAvailabilityDto(
            Guid EventTicketTypeId,
            string Name,
            decimal Price,
            int Capacity,
            int Sold,
            int Available);

        public sealed record EventListItemDto(
            Guid EventId,
            string Title,
            DateTimeOffset StartsAt,
            Guid VenueId,
            string VenueName,
            IReadOnlyList<TicketTypeAvailabilityDto> Tickets);

// Simple paging container
        public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

// Query params for filtering/paging
        public sealed record PublicEventsQuery(
            DateTimeOffset? From = null,
            DateTimeOffset? To = null,
            Guid? VenueId = null,
            string? Search = null,
            int Page = 1,
            int PageSize = 20);
        
        public sealed record CreateReservationRequestDto(
            Guid EventTicketTypeId,
            int Quantity,
            string IdempotencyKey,
            int HoldSeconds = 600 // default 10 minutes //this should be configurable. 
        );

        public sealed record CreateReservationResultDto(
            Guid ReservationId,
            Guid EventTicketTypeId,
            int Quantity,
            DateTimeOffset ExpiresAt,
            ReservationStatus Status
        );
        
        public sealed record PurchaseRequestDto(
            Guid ReservationId,
            string PaymentToken,      // mock token/nonce you’d get from a UI
            string Currency = "USD",
            string? IdempotencyKey = null // optional (future: purchase idempotency)
        );

        public sealed record PurchaseResultDto(
            Guid ReservationId,
            Guid EventTicketTypeId,
            int Quantity,
            decimal UnitPrice,
            decimal TotalAmount,
            DateTimeOffset PurchasedAt
        );

        public sealed record PaymentProcessorRequestDto(
            decimal Amount,
            string Currency,
            string Description,
            string PaymentToken,
            string? IdempotencyKey);
        
    
    }
}