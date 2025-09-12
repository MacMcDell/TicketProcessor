using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TicketProcessor.Domain;
using Npgsql.EntityFrameworkCore.PostgreSQL; 

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
    }
}
