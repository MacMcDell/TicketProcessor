using FluentAssertions;
using TicketProcessor.Application.Validation;
using TicketProcessor.Domain;
using Xunit;

namespace TicketProcessor.Tests;

public class CreateEventDtoTests
{
    private static CreateEventDto BuildRequest(
        string venueName,
        int venueCapacity,
        string title,
        params (string name, decimal price, int capacity)[] tickets)
    {
        var ticketDtos = tickets.Select(t =>
            new CreateEventTicketTypeDto(
                Name: t.name,
                Price: t.price,
                Capacity: t.capacity
            )
        ).ToList();

        return new CreateEventDto(
            VenueId: null,
            VenueName: venueName,
            VenueCapacity: venueCapacity,
            StartsAt: DateTimeOffset.UtcNow.AddDays(7),
            Title: title,
            Description: "desc",
            TicketTypes: ticketDtos
        );
    }

    [Fact]
    public async Task CreateEvent_WithSingleTicketType_WritesAllRows()
    {
        await using var fx = new TestFixture();

        var validator = new CreateEventValidation();
        var req = BuildRequest(
            venueName: "Test Hall",
            venueCapacity: 500,
            title: "Solo Night",
            tickets: new (string, decimal, int)[] { ("GA", 39.99m, 400) });

        // Validate first (mimics Function behavior)
        var result = await validator.ValidateAsync(req);
        result.IsValid.Should().BeTrue(result.ToString());

        var evt = await fx.EventService.CreateEventAsync(req);

        // Assertions on DTO
        evt.Id.Should().NotBeEmpty();
        evt.VenueId.Should().NotBeEmpty();
        evt.Title.Should().Be("Solo Night");

        // Verify Db persisted rows
        fx.Db.Venues.Count().Should().Be(1);
        fx.Db.Events.Count().Should().Be(1);
        fx.Db.EventTicketTypes.Count().Should().Be(1);

        var ett = fx.Db.EventTicketTypes.Single();
        ett.Capacity.Should().Be(400);
        ett.Sold.Should().Be(0);
    }

    [Fact]
    public async Task CreateEvent_WithTwoTicketTypes_WritesAllRows()
    {
        await using var fx = new TestFixture();

        var validator = new CreateEventValidation();
        var req = BuildRequest(
            venueName: "Big Room",
            venueCapacity: 1000,
            title: "Double Feature",
            tickets: new (string, decimal, int)[] {
                ("GA", 49.99m, 850),
                ("VIP", 129.00m, 150)
            });

        var result = await validator.ValidateAsync(req);
        result.IsValid.Should().BeTrue(result.ToString());

        var evt = await fx.EventService.CreateEventAsync(req);

        evt.Title.Should().Be("Double Feature");
        fx.Db.Venues.Count().Should().Be(1);
        fx.Db.Events.Count().Should().Be(1);
        fx.Db.EventTicketTypes.Count().Should().Be(2);

        var vip = fx.Db.EventTicketTypes.Single(x => x.Capacity == 150);
        vip.Sold.Should().Be(0);
    }

    [Fact]
    public async Task CreateEvent_ValidationFails_WhenVenueMissing()
    {
        // Missing VenueId AND missing (VenueName/VenueCapacity) should fail validation
        var req = new CreateEventDto(
            VenueId: null,
            VenueName: null,
            VenueCapacity: null,
            StartsAt: DateTimeOffset.UtcNow.AddDays(7),
            Title: "Bad Event",
            Description: null,
            TicketTypes: new List<CreateEventTicketTypeDto> {
                new ("GA", 10, 100)
            }
        );

        var validator = new CreateEventValidation();
        var result = await validator.ValidateAsync(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain(x => x.Contains("Venue"));
    }

}
