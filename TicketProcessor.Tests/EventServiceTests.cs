using Xunit;
using FluentAssertions;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;
using Moq;
using Microsoft.EntityFrameworkCore;
using TicketProcessor.Application.Services;

namespace TicketProcessor.Tests;

public class EventServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock;

    public EventServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
        _idempotencyServiceMock = new Mock<IIdempotencyService>();
    }

    private async Task ResetDatabaseAsync()
    {
        await _fixture.Db.Database.EnsureDeletedAsync();
        await _fixture.Db.Database.EnsureCreatedAsync();
    }


    private IEventService GetEventService(IIdempotencyService? idempotencyService = null,
        IReservationRepository? reservationRepository = null)
    {
        return new EventService(
            _fixture.Venues,
            _fixture.Events,
            _fixture.EventTicketTypes,
            _fixture.Uow,
            _fixture.Mapper,
            reservationRepository ?? _fixture.Reservations,
            idempotencyService ?? _idempotencyServiceMock.Object,
            _fixture.PaymentGateway,
            _fixture._logger
        );
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldCreateReservation_WhenTicketsAvailableAndValidRequest()
    {
        await ResetDatabaseAsync();
        var eventService = GetEventService(_idempotencyServiceMock.Object);

        var eventId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var eventTicketTypeId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var quantity = 2;
        var holdSeconds = 30;


        await _fixture.Db.Venues.AddAsync(new Venue { Id = venueId, Name = "Test Venue", Capacity = 100 });
        await _fixture.Db.Events.AddAsync(new Event
            { Id = eventId, VenueId = venueId, Title = "Test Event", StartsAt = DateTimeOffset.UtcNow.AddDays(1) });
        await _fixture.Db.EventTicketTypes.AddAsync(new EventTicketType
        {
            Id = eventTicketTypeId,
            EventId = eventId,
            Name = "Standard",
            Price = 10m,
            Capacity = 5,
            Sold = 0
        });
        await _fixture.Db.SaveChangesAsync();

        var request = new CreateReservationRequestDto
        (
            EventTicketTypeId: eventTicketTypeId,
            Quantity: quantity,
            HoldSeconds: holdSeconds,
            IdempotencyKey: idempotencyKey
        );


        _idempotencyServiceMock
            .Setup(x => x.TrySetAsync(
                It.Is<string>(k => k == idempotencyKey),
                It.IsAny<Guid>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));


        var result = await eventService.CreateReservationAsync(request, CancellationToken.None);


        result.Should().NotBeNull();
        result.ReservationId.Should().NotBeEmpty();
        result.EventTicketTypeId.Should().Be(eventTicketTypeId);
        result.Quantity.Should().Be(quantity);
        result.Status.Should().Be(ReservationStatus.Pending);
        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);


        var savedReservation = await _fixture.Db.Reservations.FirstOrDefaultAsync(r => r.Id == result.ReservationId);
        savedReservation.Should().NotBeNull();
        savedReservation!.EventTicketTypeId.Should().Be(eventTicketTypeId);
        savedReservation.Quantity.Should().Be(quantity);
        savedReservation.Status.Should().Be(ReservationStatus.Pending);
        savedReservation.IdempotencyKey.Should().Be(idempotencyKey);
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldThrowException_WhenQuantityIsZero()
    {
        await ResetDatabaseAsync();
        var eventService = GetEventService();

        var request = new CreateReservationRequestDto
        (
            EventTicketTypeId: Guid.NewGuid(),
            Quantity: 0,
            HoldSeconds: 30,
            IdempotencyKey: Guid.NewGuid().ToString()
        );


        Func<Task> action = async () => await eventService.CreateReservationAsync(request, CancellationToken.None);


        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Quantity must be greater than 0.");
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldThrowException_WhenQuantityIsNegative()
    {
        await ResetDatabaseAsync();
        var eventService = GetEventService();

        var request = new CreateReservationRequestDto
        (
            EventTicketTypeId: Guid.NewGuid(),
            Quantity: -1,
            HoldSeconds: 30,
            IdempotencyKey: Guid.NewGuid().ToString()
        );


        Func<Task> action = async () => await eventService.CreateReservationAsync(request, CancellationToken.None);


        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Quantity must be greater than 0.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateReservationAsync_ShouldThrowException_WhenIdempotencyKeyIsMissing(string idempotencyKey)
    {
        await ResetDatabaseAsync();
        var eventService = GetEventService();

        var request = new CreateReservationRequestDto
        (
            EventTicketTypeId: Guid.NewGuid(),
            Quantity: 1,
            HoldSeconds: 30,
            IdempotencyKey: idempotencyKey
        );


        Func<Task> action = async () => await eventService.CreateReservationAsync(request, CancellationToken.None);


        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IdempotencyKey is required.");
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldThrowException_WhenEventTicketTypeNotFound()
    {
        await ResetDatabaseAsync();

        _idempotencyServiceMock.Invocations.Clear();
        var eventService = GetEventService(_idempotencyServiceMock.Object);

        var nonExistentEventTicketTypeId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var quantity = 1;

        var request = new CreateReservationRequestDto
        (
            EventTicketTypeId: nonExistentEventTicketTypeId,
            Quantity: quantity,
            HoldSeconds: 30,
            IdempotencyKey: idempotencyKey
        );


        _idempotencyServiceMock
            .Setup(x => x.TrySetAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));


        Func<Task> action = async () => await eventService.CreateReservationAsync(request, CancellationToken.None);


        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Event ticket type not found.");
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldThrowException_WhenNotEnoughTicketsAvailable()
    {
        await ResetDatabaseAsync();

        _idempotencyServiceMock.Invocations.Clear();

        var eventId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var eventTicketTypeId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var quantityToRequest = 3;


        await _fixture.Db.Venues.AddAsync(new Venue { Id = venueId, Name = "Test Venue", Capacity = 100 });
        await _fixture.Db.Events.AddAsync(new Event
            { Id = eventId, VenueId = venueId, Title = "Test Event", StartsAt = DateTimeOffset.UtcNow.AddDays(1) });
        await _fixture.Db.EventTicketTypes.AddAsync(new EventTicketType
        {
            Id = eventTicketTypeId,
            EventId = eventId,
            Name = "Standard",
            Price = 10m,
            Capacity = 2,
            Sold = 0
        });
        await _fixture.Db.SaveChangesAsync();

        var request = new CreateReservationRequestDto
        (
            EventTicketTypeId: eventTicketTypeId,
            Quantity: quantityToRequest,
            HoldSeconds: 30,
            IdempotencyKey: idempotencyKey
        );


        _idempotencyServiceMock
            .Setup(x => x.TrySetAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));


        var reservationsMock = new Mock<IReservationRepository>();
        reservationsMock.Setup(r => r.CountActivePendingAsync(
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var eventService = GetEventService(_idempotencyServiceMock.Object, reservationsMock.Object);


        Func<Task> action = async () => await eventService.CreateReservationAsync(request, CancellationToken.None);


        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Not enough tickets available.");
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldThrowException_WhenNotEnoughTicketsDueToPendingHolds()
    {
        await ResetDatabaseAsync();

        _idempotencyServiceMock.Invocations.Clear();

        var eventId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var eventTicketTypeId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var quantityToRequest = 2;


        await _fixture.Db.Venues.AddAsync(new Venue { Id = venueId, Name = "Test Venue", Capacity = 100 });
        await _fixture.Db.Events.AddAsync(new Event
            { Id = eventId, VenueId = venueId, Title = "Test Event", StartsAt = DateTimeOffset.UtcNow.AddDays(1) });
        await _fixture.Db.EventTicketTypes.AddAsync(new EventTicketType
        {
            Id = eventTicketTypeId,
            EventId = eventId,
            Name = "Standard",
            Price = 10m,
            Capacity = 5,
            Sold = 1
        });
        await _fixture.Db.SaveChangesAsync();

        var request = new CreateReservationRequestDto
        (
            EventTicketTypeId: eventTicketTypeId,
            Quantity: quantityToRequest,
            HoldSeconds: 30,
            IdempotencyKey: idempotencyKey
        );


        _idempotencyServiceMock
            .Setup(x => x.TrySetAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));


        var reservationsMock = new Mock<IReservationRepository>();
        reservationsMock.Setup(r => r.CountActivePendingAsync(
                It.Is<Guid>(id => id == eventTicketTypeId),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var eventService = GetEventService(_idempotencyServiceMock.Object, reservationsMock.Object);


        Func<Task> action = async () => await eventService.CreateReservationAsync(request, CancellationToken.None);


        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Not enough tickets available.");
    }

    [Fact]
    public async Task
        CreateReservationAsync_ShouldReturnExistingReservation_WhenDuplicateIdempotencyKeyAndReservationExists()
    {
        await ResetDatabaseAsync();

        _idempotencyServiceMock.Invocations.Clear();

        var eventId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var eventTicketTypeId = Guid.NewGuid();
        var existingReservationId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var quantity = 2;
        var holdSeconds = 30;
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddSeconds(holdSeconds);


        await _fixture.Db.Venues.AddAsync(new Venue { Id = venueId, Name = "Test Venue", Capacity = 100 });
        await _fixture.Db.Events.AddAsync(new Event
            { Id = eventId, VenueId = venueId, Title = "Test Event", StartsAt = DateTimeOffset.UtcNow.AddDays(1) });
        await _fixture.Db.EventTicketTypes.AddAsync(new EventTicketType
        {
            Id = eventTicketTypeId,
            EventId = eventId,
            Name = "Standard",
            Price = 10m,
            Capacity = 5,
            Sold = 0
        });

        await _fixture.Db.Reservations.AddAsync(new Reservation
        {
            Id = existingReservationId,
            EventTicketTypeId = eventTicketTypeId,
            Quantity = quantity,
            Status = ReservationStatus.Pending,
            ExpiresAt = expiresAt,
            IdempotencyKey = idempotencyKey
        });
        await _fixture.Db.SaveChangesAsync();

        var request = new CreateReservationRequestDto
        (
            EventTicketTypeId: eventTicketTypeId,
            Quantity: quantity,
            HoldSeconds: holdSeconds,
            IdempotencyKey: idempotencyKey
        );


        _idempotencyServiceMock
            .Setup(x => x.TrySetAsync(
                It.Is<string>(k => k == idempotencyKey),
                It.IsAny<Guid>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, existingReservationId));

        var eventService = GetEventService(_idempotencyServiceMock.Object);


        var result = await eventService.CreateReservationAsync(request, CancellationToken.None);


        result.Should().NotBeNull();
        result.ReservationId.Should().Be(existingReservationId);
        result.EventTicketTypeId.Should().Be(eventTicketTypeId);
        result.Quantity.Should().Be(quantity);
        result.Status.Should().Be(ReservationStatus.Pending);
        result.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldThrowException_WhenDuplicateIdempotencyKeyButReservationNotFound()
    {
        await ResetDatabaseAsync();

        _idempotencyServiceMock.Invocations.Clear();

        var eventId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var eventTicketTypeId = Guid.NewGuid();
        var nonExistentExistingReservationId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var quantity = 2;
        var holdSeconds = 30;


        await _fixture.Db.Venues.AddAsync(new Venue { Id = venueId, Name = "Test Venue", Capacity = 100 });
        await _fixture.Db.Events.AddAsync(new Event
            { Id = eventId, VenueId = venueId, Title = "Test Event", StartsAt = DateTimeOffset.UtcNow.AddDays(1) });
        await _fixture.Db.EventTicketTypes.AddAsync(new EventTicketType
        {
            Id = eventTicketTypeId,
            EventId = eventId,
            Name = "Standard",
            Price = 10m,
            Capacity = 5,
            Sold = 0
        });
        await _fixture.Db.SaveChangesAsync();

        var request = new CreateReservationRequestDto
        (
            EventTicketTypeId: eventTicketTypeId,
            Quantity: quantity,
            HoldSeconds: holdSeconds,
            IdempotencyKey: idempotencyKey
        );


        _idempotencyServiceMock
            .Setup(x => x.TrySetAsync(
                It.Is<string>(k => k == idempotencyKey),
                It.IsAny<Guid>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, nonExistentExistingReservationId));

        var eventService = GetEventService(_idempotencyServiceMock.Object);


        Func<Task> action = async () => await eventService.CreateReservationAsync(request, CancellationToken.None);


        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Duplicate idempotency key.");
    }
}