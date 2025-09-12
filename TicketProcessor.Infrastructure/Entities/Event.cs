using TicketProcessor.Infrastructure;

namespace TicketProcessor.Domain;

public class Event : BaseProperties
{
    public Guid VenueId { get; set; }
    public Venue? Venue { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public ICollection<EventTicketType> TicketTypes { get; set; } = new List<EventTicketType>();
    
}