using TicketProcessor.Infrastructure;

namespace TicketProcessor.Domain;

public class EventTicketType : BaseProperties
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public int Capacity { get; set; }             // capacity allocated to this ticket type for the event
    public int Sold { get; set; }                 // optional running total
    
}