namespace TicketProcessor.Domain
{
    #region Event DTOs

    /// <summary>
    /// create event ticket type for existing event
    /// </summary>
    public record AddEventTicketTypeDto
    {
        public Guid EventId { get; set; }
        public int Capacity { get; init; }
        public int Sold { get; init; }
        public string Name { get; init; } = default!;
        public decimal Price { get; init; }
    }

    /// <summary>
    /// Get event ticket type for existing event
    /// </summary>
    public record EventTicketTypeDto
    {
        public Guid? Id { get; init; }
        public Guid EventId { get; set; }
        public int Capacity { get; init; }
        public int Sold { get; set; }
        public string Name { get; init; } = default!;
        public decimal Price { get; init; }
    }

    /// <summary>
    /// create event for existing venue
    /// </summary>
    public record EventDto
    {
        public Guid? Id { get; set; }
        public Guid VenueId { get; set; }
        public DateTimeOffset StartsAt { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    };

    /// <summary>
    /// create new event and new venue if necessar and new ticket types for existing event
    /// </summary>
    /// <param name="VenueId"></param>
    /// <param name="VenueName"></param>
    /// <param name="VenueCapacity"></param>
    /// <param name="StartsAt"></param>
    /// <param name="Title"></param>
    /// <param name="Description"></param>
    /// <param name="TicketTypes"></param>
    public record CreateEventDto(
        Guid? VenueId,
        string? VenueName,
        int? VenueCapacity,
        DateTimeOffset StartsAt,
        string Title,
        string? Description,
        IReadOnlyList<CreateEventTicketTypeDto> TicketTypes);

    /// <summary>
    /// used with CreateEventDto
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Price"></param>
    /// <param name="Capacity"></param>
    public record CreateEventTicketTypeDto(string Name, decimal Price, int Capacity);

    /// <summary>
    /// return event list item dto
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="Title"></param>
    /// <param name="StartsAt"></param>
    /// <param name="VenueId"></param>
    /// <param name="VenueName"></param>
    /// <param name="Tickets"></param>
    public sealed record EventListItemDto(
        Guid EventId,
        string Title,
        DateTimeOffset StartsAt,
        Guid VenueId,
        string VenueName,
        IReadOnlyList<TicketTypeAvailabilityDto> Tickets);

    #endregion

    #region Venue DTOs

    /// <summary>
    /// update venue
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="Name"></param>
    /// <param name="Capacity"></param>
    public record VenueDto
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
    }

    #endregion

    #region Purchase DTOs

    /// <summary>
    /// complete a purchase with the reservation id and payment token you got back from the payment processor.
    /// </summary>
    /// <param name="ReservationId"></param>
    /// <param name="PaymentToken"></param>
    /// <param name="Currency"></param>
    /// <param name="IdempotencyKey"></param>
    public sealed record PurchaseRequestDto(
        Guid ReservationId,
        string PaymentToken, // mock token/nonce you’d get from a UI
        string Currency = "USD",
        string? IdempotencyKey = null // optional (future: purchase idempotency)
    );

    /// <summary>
    /// return the result of the purchase with the reservation id and payment token you got back from the payment processor.
    /// </summary>
    /// <param name="ReservationId"></param>
    /// <param name="EventTicketTypeId"></param>
    /// <param name="Quantity"></param>
    /// <param name="UnitPrice"></param>
    /// <param name="TotalAmount"></param>
    /// <param name="PurchasedAt"></param>
    public sealed record PurchaseResultDto(
        Guid ReservationId,
        Guid EventTicketTypeId,
        int Quantity,
        decimal UnitPrice,
        decimal TotalAmount,
        DateTimeOffset PurchasedAt
    );

    /// <summary>
    /// what the payment processor expects.
    /// </summary>
    /// <param name="Amount"></param>
    /// <param name="Currency"></param>
    /// <param name="Description"></param>
    /// <param name="PaymentToken"></param>
    /// <param name="IdempotencyKey"></param>
    public sealed record PaymentProcessorRequestDto(
        decimal Amount,
        string Currency,
        string Description,
        string PaymentToken,
        string? IdempotencyKey);

    #endregion

    #region Reservation DTOs

    /// <summary>
    /// what the customer sees.
    /// </summary>
    public record ReservationResponseDto
    {
        public Guid Id { get; init; }
        public Guid EventTicketTypeId { get; init; }
        public int Quantity { get; init; }
        public ReservationStatus Status { get; set; }
        public DateTimeOffset ExpiresAt { get; init; }
        public string? IdempotencyKey { get; init; }
    };

    /// <summary>
    /// the reservation request.
    /// </summary>
    /// <param name="EventTicketTypeId"></param>
    /// <param name="Quantity"></param>
    /// <param name="IdempotencyKey">this would be built on the FE with local storage or something</param>
    /// <param name="HoldSeconds"> just for testeing.. this allows us to tweak how long to hold for</param>
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

    #endregion

    #region Ticket DTOs

    /// <summary>
    /// the ticet type availability dto. 
    /// </summary>
    /// <param name="EventTicketTypeId">required for editing</param>
    /// <param name="Name"></param>
    /// <param name="Price"></param>
    /// <param name="Capacity"></param>
    /// <param name="Sold"></param>
    /// <param name="Available"></param>
    public sealed record TicketTypeAvailabilityDto(
        Guid EventTicketTypeId,
        string Name,
        decimal Price,
        int Capacity,
        int Sold,
        int Available);

    #endregion

    #region Other

    // Simple paging container
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

    // Query params for filtering/paging
    public sealed record PageQuery(
        DateTimeOffset? From = null,
        DateTimeOffset? To = null,
        Guid? VenueId = null,
        string? Search = null,
        int Page = 1,
        int PageSize = 20);

    #endregion
}