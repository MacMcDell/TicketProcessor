using Microsoft.EntityFrameworkCore;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;
using TicketProcessor.Domain.Dto;

namespace TicketProcessor.Infrastructure.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly TicketingDbContext _db;
    public ReservationRepository(TicketingDbContext db) => _db = db;

    public async Task AddAsync(ReservationDto reservation, CancellationToken ct)
    {
        var entity = new Reservation
        {
            Id = reservation.Id,
            EventTicketTypeId = reservation.EventTicketTypeId,
            Quantity = reservation.Quantity,
            Status = reservation.Status,
            ExpiresAt = reservation.ExpiresAt,
            IdempotencyKey = reservation.IdempotencyKey
        };
        await _db.Reservations.AddAsync(entity, ct);
    }

    public Task<ReservationDto?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Reservations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReservationDto{
                Id = r.Id, 
                EventTicketTypeId = r.EventTicketTypeId, 
                Quantity = r.Quantity, 
                Status = r.Status, 
                ExpiresAt = r.ExpiresAt, 
                IdempotencyKey = r.IdempotencyKey}
            )
            .FirstOrDefaultAsync(ct);

    public Task<int> CountActivePendingAsync(Guid eventTicketTypeId, DateTimeOffset now, CancellationToken ct)
        => _db.Reservations.AsNoTracking()
            .Where(r => r.EventTicketTypeId == eventTicketTypeId && r.Status == ReservationStatus.Pending && r.ExpiresAt > now)
            .SumAsync(r => r.Quantity, ct);

    public async Task<ReservationDto?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
    {
        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == id, ct);
        return r is null
            ? null
            : new ReservationDto
            {
               Id = r.Id, 
               EventTicketTypeId = r.EventTicketTypeId, 
               Quantity = r.Quantity, 
               Status = r.Status, 
               ExpiresAt = r.ExpiresAt, 
               IdempotencyKey = r.IdempotencyKey
            };
    }
    public async Task UpdateAsync(ReservationDto reservation, CancellationToken ct)
    {
        // Use the tracked entity if present; otherwise load and track it
        var e = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservation.Id, ct)
                ?? throw new InvalidOperationException("Reservation not found.");
        
        e.Id = reservation.Id;
        e.EventTicketTypeId = reservation.EventTicketTypeId;
        e.Quantity = reservation.Quantity;
        e.Status = reservation.Status;
        e.ExpiresAt = reservation.ExpiresAt;
        e.IdempotencyKey = reservation.IdempotencyKey;
        
        _db.Attach(e);
        _db.Entry(e).Property(x => x.Status).IsModified = true;
        _db.Entry(e).Property(x => x.ExpiresAt).IsModified = true;
       
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var recordToDelete = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (recordToDelete != null)
        {
            recordToDelete.Status = ReservationStatus.Cancelled;
            _db.Reservations.Remove(recordToDelete);
        }
        
    }
}