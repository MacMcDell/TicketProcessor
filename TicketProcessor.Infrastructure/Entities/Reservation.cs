using TicketProcessor.Infrastructure;

namespace TicketProcessor.Domain;

public class Reservation : BaseProperties
{
    public Guid EventTicketTypeId { get; set; }
    public EventTicketType? EventTicketType { get; set; }

    public int Quantity { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? IdempotencyKey { get; set; } // can mirror Redis key for tracing
}