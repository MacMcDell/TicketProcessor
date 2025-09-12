using TicketProcessor.Infrastructure;

namespace TicketProcessor.Domain;

public class Venue : BaseProperties
{
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    
}