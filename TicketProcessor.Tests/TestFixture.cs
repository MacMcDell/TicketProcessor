using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Application.Services;
using TicketProcessor.Infrastructure;
using TicketProcessor.Infrastructure.Mapping;
using TicketProcessor.Infrastructure.PaymentProcessors;
using TicketProcessor.Infrastructure.Repositories;

namespace TicketProcessor.Tests;

public sealed class TestFixture : IAsyncDisposable
{
    public NullLogger<EventService> _logger = new NullLogger<EventService>();
    public TicketingDbContext Db { get; }
    public IMapper Mapper { get; }

    // Concrete EF repos + UoW
    public IVenueRepository Venues { get; }
    public IReservationRepository Reservations { get; set; }
    public IEventRepository Events { get; }
    public IPaymentGateway PaymentGateway { get; }
    public IEventTicketTypeRepository EventTicketTypes { get; }
    public IUnitOfWork Uow { get; }

    private HttpClient _client = new HttpClient();

    private IIdempotencyService _mockIdempotency = new Mock<IIdempotencyService>().Object; 

    public IEventService EventService { get; }

    // Modify constructor to generate dbName internally
    public TestFixture()
    {
        var dbName = Guid.NewGuid().ToString(); // Generate a unique name for the in-memory database
        // InMemory DbContext
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        Db = new TicketingDbContext(options);

        // AutoMapper
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<EntityToDtoProfile>();
        }, new LoggerFactory());
        mapperConfig.AssertConfigurationIsValid();
        Mapper = mapperConfig.CreateMapper();
        
        

        // EF repos
        Venues = new VenueRepository(Db, Mapper);
        Events = new EventRepository(Db, Mapper);
        PaymentGateway = new FakePaymentProcessor(_client);
        Reservations = new ReservationRepository(Db); 
        EventTicketTypes = new EventTicketTypeRepository(Db, Mapper);
        Uow = new UnitOfWork(Db);

        // App service
        EventService = new EventService(Venues, Events, EventTicketTypes, Uow, Mapper, Reservations, _mockIdempotency, PaymentGateway,_logger );
    }

    public ValueTask DisposeAsync()
    {
        Db.Dispose();
        return ValueTask.CompletedTask;
    }
}