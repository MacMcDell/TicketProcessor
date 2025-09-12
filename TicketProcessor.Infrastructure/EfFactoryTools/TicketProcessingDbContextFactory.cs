using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TicketProcessor.Infrastructure.EfFactoryTools;

public class TicketProcessingDbContextFactory  : IDesignTimeDbContextFactory<TicketingDbContext>
{
    public TicketingDbContext CreateDbContext(string[] args)
    {
        var cfg = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("local.settings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = cfg.GetConnectionString("Postgres")
                 ?? Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                 ?? "Host=localhost;Port=5432;Database=ticketing;Username=postgres;Password=postgres;Include Error Detail=true;";

        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseNpgsql(cs, sql => sql.MigrationsAssembly(typeof(TicketingDbContext).Assembly.FullName))
            .Options;

        return new TicketingDbContext(options);
    }
}