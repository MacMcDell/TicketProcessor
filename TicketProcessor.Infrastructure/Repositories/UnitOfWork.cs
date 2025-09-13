using TicketProcessor.Application.Interfaces;

namespace TicketProcessor.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly TicketingDbContext _db;
    public UnitOfWork(TicketingDbContext db) => _db = db;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}