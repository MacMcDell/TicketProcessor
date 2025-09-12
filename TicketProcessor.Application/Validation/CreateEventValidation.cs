using FluentValidation;
using TicketProcessor.Domain;

namespace TicketProcessor.Application.Validation;

public class CreateEventValidation : AbstractValidator<CreateEventDto>
{
    public CreateEventValidation()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => new { x.VenueId, x.VenueName, x.VenueCapacity })
            .Must(v => v.VenueId.HasValue || (!string.IsNullOrWhiteSpace(v.VenueName) && v.VenueCapacity is >= 1))
            .WithMessage("Provide either VenueId OR (VenueName and VenueCapacity >= 1).");

        RuleFor(x => x.TicketTypes).NotNull().Must(ts => ts.Count > 0)
            .WithMessage("At least one ticket type is required.");

        RuleForEach(x => x.TicketTypes).ChildRules(t =>
        {
            t.RuleFor(y => y.Name).NotEmpty().MaximumLength(100);
            t.RuleFor(y => y.Price).GreaterThanOrEqualTo(0);
            t.RuleFor(y => y.Capacity).GreaterThan(0);
        });
        
       RuleFor(x => x.TicketTypes)
            .Must(list => list.Select(t => t.Name.Trim().ToLowerInvariant()).Distinct().Count() == list.Count)
            .WithMessage("Ticket type names must be unique within the event.");
    }
}