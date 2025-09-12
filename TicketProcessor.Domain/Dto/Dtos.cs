namespace TicketProcessor.Domain.Dto;


    public record VenueDto(Guid Id, string Name, int Capacity);

    public record EventDto
    {
        public Guid? Id { get; set; }
        public Guid VenueId { get; set; }
        public DateTimeOffset StartsAt { get; set; }
        public string Title { get; set; } = string.Empty; 
        public string? Description { get; set; }
    };

    /// <summary>
/// need this way to allow for null id when creating new tickets. 
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

public record AddEventTicketTypeDto
    {
        public Guid EventId { get; set; }
        public int Capacity { get; init; }
        public int Sold { get; init; }
        public string Name { get; init; } = default!;
        public decimal Price { get; init; }
    }

public record ReservationDto
{
public Guid Id { get;set; }
public Guid EventTicketTypeId { get;set; }
public int Quantity { get;set; }
public ReservationStatus Status { get;set; }
public DateTimeOffset ExpiresAt { get;set; }
public string? IdempotencyKey { get;init; }
};
