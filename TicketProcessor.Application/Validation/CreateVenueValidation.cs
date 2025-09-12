using FluentValidation;
using TicketProcessor.Domain;

namespace TicketProcessor.Application.Validation;

public class CreateVenueValidation : AbstractValidator<VenueDto>
{
    public CreateVenueValidation()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("Venue name is required and must be less than 200 characters.");
        RuleFor(x => x.Capacity).GreaterThan(0).WithMessage("Must be greater than 0.");

    }
}