using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TicketProcessor.Domain;


namespace TicketProcessor.Infrastructure;

public class TicketingDbContext(DbContextOptions<TicketingDbContext> options)
    : DbContext(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventTicketType> EventTicketTypes => Set<EventTicketType>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseProperties>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Created = now;
                    entry.Entity.LastModified = now;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    // prevent CreatedOn from being modified
                    entry.Property(x => x.Created).IsModified = false;
                    entry.Entity.LastModified = now;
                    break;
                case EntityState.Deleted:
                    // convert hard delete into soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.LastModified = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        foreach (var entityType in b.Model.GetEntityTypes())
        {
            if (typeof(BaseProperties).IsAssignableFrom(entityType.ClrType))
            {
                // Add a global query filter: IsDeleted == false
                var param = Expression.Parameter(entityType.ClrType, "e");
                var prop = Expression.Property(param, nameof(BaseProperties.IsDeleted));
                var body = Expression.Equal(prop, Expression.Constant(false));
                var lambda = Expression.Lambda(body, param);
                entityType.SetQueryFilter(lambda);
            }
        }

        b.Entity<Venue>(e =>
        {
            e.ToTable("venues");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");
        });

        b.Entity<Event>(e =>
        {
            e.ToTable("events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.HasOne(x => x.Venue).WithMany().HasForeignKey(x => x.VenueId);
            e.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");
        });


        b.Entity<EventTicketType>(e =>
        {
            e.ToTable("event_ticket_types");
            e.HasKey(x => x.Id);

            e.HasOne(x => x.Event).WithMany(x => x.TicketTypes)
                .HasForeignKey(x => x.EventId);

            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Price).HasColumnType("numeric(12,2)");

            e.HasIndex(x => new { x.EventId, x.Name }).IsUnique();

            e.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");
        });

        b.Entity<Reservation>(e =>
        {
            e.ToTable("reservations");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.EventTicketType).WithMany().HasForeignKey(x => x.EventTicketTypeId);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");
            e.HasIndex(x => x.IdempotencyKey);
        });

        base.OnModelCreating(b);
        SeedInitialData(b);
    }

    private static void SeedInitialData(ModelBuilder b)
{
    var created = new DateTimeOffset(2025, 09, 01, 0, 0, 0, TimeSpan.Zero);
    var starts1 = new DateTimeOffset(2025, 10, 01, 20, 0, 0, TimeSpan.Zero);
    var starts2 = new DateTimeOffset(2025, 10, 08, 20, 0, 0, TimeSpan.Zero);
    var starts3 = new DateTimeOffset(2025, 10, 15, 20, 0, 0, TimeSpan.Zero);
    var holdExp  = new DateTimeOffset(2025, 10, 01, 21, 0, 0, TimeSpan.Zero);

    var v1  = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var v2  = Guid.Parse("22222222-2222-2222-2222-222222222222");
    var e1  = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    var e2  = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    var e3  = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    var ett1= Guid.Parse("d1111111-1111-1111-1111-111111111111");
    var ett2= Guid.Parse("d2222222-2222-2222-2222-222222222222");
    var ett3= Guid.Parse("d3333333-3333-3333-3333-333333333333");
    var ett4= Guid.Parse("d4444444-4444-4444-4444-444444444444");
    var ett5= Guid.Parse("d5555555-5555-5555-5555-555555555555");
    var ett6= Guid.Parse("d6666666-6666-6666-6666-666666666666");
    var ett7= Guid.Parse("d7777777-7777-7777-7777-777777777777");
    var res1= Guid.Parse("99999999-9999-9999-9999-999999999999");

    // Venues
    b.Entity<Venue>().HasData(
        new { Id = v1, Name = "Commodore Ballroom", Capacity = 900,  Created = created, LastModified = created, IsDeleted = false },
        new { Id = v2, Name = "Queen Elizabeth Theatre", Capacity = 2765, Created = created, LastModified = created, IsDeleted = false }
    );

    // Events
    b.Entity<Event>().HasData(
        new { Id = e1, VenueId = v1, Title = "The Alpines",    StartsAt = starts1, Description = "Tour kickoff", Created = created, LastModified = created, IsDeleted = false },
        new { Id = e2, VenueId = v1, Title = "DJ Night",       StartsAt = starts2, Description = (string?)null,  Created = created, LastModified = created, IsDeleted = false },
        new { Id = e3, VenueId = v2, Title = "Symphonic Rock", StartsAt = starts3, Description = (string?)null,  Created = created, LastModified = created, IsDeleted = false }
    );

    // EventTicketTypes
    b.Entity<EventTicketType>().HasData(
        new { Id = ett1, EventId = e1, Name = "GA",      Price = 49.99m,  Capacity = 800,  Sold = 0, Created = created, LastModified = created, IsDeleted = false },
        new { Id = ett2, EventId = e1, Name = "VIP",     Price = 129.00m, Capacity = 100,  Sold = 0, Created = created, LastModified = created, IsDeleted = false },
        new { Id = ett3, EventId = e2, Name = "GA",      Price = 39.00m,  Capacity = 850,  Sold = 0, Created = created, LastModified = created, IsDeleted = false },
        new { Id = ett4, EventId = e2, Name = "VIP",     Price = 95.00m,  Capacity = 50,   Sold = 0, Created = created, LastModified = created, IsDeleted = false },
        new { Id = ett5, EventId = e3, Name = "GA",      Price = 59.00m,  Capacity = 2200, Sold = 0, Created = created, LastModified = created, IsDeleted = false },
        new { Id = ett6, EventId = e3, Name = "VIP",     Price = 149.00m, Capacity = 300,  Sold = 0, Created = created, LastModified = created, IsDeleted = false },
        new { Id = ett7, EventId = e3, Name = "Balcony", Price = 79.00m,  Capacity = 265,  Sold = 0, Created = created, LastModified = created, IsDeleted = false }
    );

    // Sample pending reservation (shows holds in availability)
    b.Entity<Reservation>().HasData(
        new
        {
            Id = res1,
            EventTicketTypeId = ett1,
            Quantity = 3,
            Status = ReservationStatus.Pending,
            ExpiresAt = holdExp,
            IdempotencyKey = "seed-res-1",
            Created = created,
            LastModified = created,
            IsDeleted = false
        }
    );
}

}