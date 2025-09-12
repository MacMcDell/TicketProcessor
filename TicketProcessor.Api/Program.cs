using System.Text.Json;
using Azure.Core.Serialization;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Application.Services;
using TicketProcessor.Application.Validation;
using TicketProcessor.Domain.Requests;
using TicketProcessor.Infrastructure;
using TicketProcessor.Infrastructure.IDempotency;
using TicketProcessor.Infrastructure.Mapping;
using TicketProcessor.Infrastructure.PaymentProcessors;
using TicketProcessor.Infrastructure.Repositories;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(o =>
    {
        o.Serializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }) 
    .ConfigureOpenApi()
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        AddInfrastructure(services,ctx.Configuration);
        AddApplication(services);
        AddValidation(services);
        services.AddLogging();
    })
    .ConfigureLogging(lb => lb.AddConsole())
    .Build();
  

await host.RunAsync();
return;


void AddInfrastructure(IServiceCollection services, IConfiguration cfg)
{
    // EF Core (Postgres)
    var cs = cfg.GetConnectionString("Postgres")
             ?? "Host=localhost;Port=5432;Database=ticketing;Username=postgres;Password=postgres;Include Error Detail=true;";
    services.AddDbContext<TicketingDbContext>(opt =>
        opt.UseNpgsql(cs, b => b.MigrationsAssembly(typeof(TicketingDbContext).Assembly.FullName)));

    // Redis
    var redisConn = cfg["Redis:Connection"] ?? "localhost:6379";
    services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
    
    services.AddAutoMapper(cfg =>
    {
        cfg.AddProfile<EntityToDtoProfile>();
    });

}

void AddApplication(IServiceCollection services)
{
    services.AddScoped<IVenueRepository, VenueRepository>();
    services.AddScoped<IEventRepository, EventRepository>();
    services.AddScoped<IEventTicketTypeRepository, EventTicketTypeRepository>();
    services.AddScoped<IReservationRepository, ReservationRepository>();
    services.AddScoped<IIdempotencyService, IdempotencyService>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    
    services.AddScoped<IEventService, EventService>();
    services.AddScoped<IVenueService, VenueService>();
    
    // HttpClient for payment gateway
    services.AddHttpClient<IPaymentGateway, FakePaymentProcessor>();
   

}

void AddValidation(IServiceCollection services)
{
    services.AddScoped<IValidator<Request.CreateEventDto>, CreateEventValidation>();
    services.AddScoped<IValidator<Request.CreateVenueDto>, CreateVenueValidation>();
}
