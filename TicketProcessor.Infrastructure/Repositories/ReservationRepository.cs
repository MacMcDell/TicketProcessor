using Microsoft.EntityFrameworkCore;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;

namespace TicketProcessor.Infrastructure.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly TicketingDbContext _db;
    public ReservationRepository(TicketingDbContext db) => _db = db;

    public async Task AddAsync(ReservationResponseDto reservationResponse, CancellationToken ct)
    {
        var entity = new Reservation
        {
            Id = reservationResponse.Id,
            EventTicketTypeId = reservationResponse.EventTicketTypeId,
            Quantity = reservationResponse.Quantity,
            Status = reservationResponse.Status,
            ExpiresAt = reservationResponse.ExpiresAt,
            IdempotencyKey = reservationResponse.IdempotencyKey
        };
        await _db.Reservations.AddAsync(entity, ct);
    }

    public Task<ReservationResponseDto?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Reservations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReservationResponseDto
                {
                    Id = r.Id,
                    EventTicketTypeId = r.EventTicketTypeId,
                    Quantity = r.Quantity,
                    Status = r.Status,
                    ExpiresAt = r.ExpiresAt,
                    IdempotencyKey = r.IdempotencyKey
                }
            )
            .FirstOrDefaultAsync(ct);

    public Task<int> CountActivePendingAsync(Guid eventTicketTypeId, DateTimeOffset now, CancellationToken ct)
        => _db.Reservations.AsNoTracking()
            .Where(r => r.EventTicketTypeId == eventTicketTypeId && r.Status == ReservationStatus.Pending &&
                        r.ExpiresAt > now)
            .SumAsync(r => r.Quantity, ct);

    public async Task<ReservationResponseDto?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
    {
        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == id, ct);
        return r is null
            ? null
            : new ReservationResponseDto
            {
                Id = r.Id,
                EventTicketTypeId = r.EventTicketTypeId,
                Quantity = r.Quantity,
                Status = r.Status,
                ExpiresAt = r.ExpiresAt,
                IdempotencyKey = r.IdempotencyKey
            };
    }

    public async Task UpdateAsync(ReservationResponseDto reservationResponse, CancellationToken ct)
    {
        // Use the tracked entity if present; otherwise load and track it
        var e = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservationResponse.Id, ct)
                ?? throw new InvalidOperationException("Reservation not found.");

        e.Id = reservationResponse.Id;
        e.EventTicketTypeId = reservationResponse.EventTicketTypeId;
        e.Quantity = reservationResponse.Quantity;
        e.Status = reservationResponse.Status;
        e.ExpiresAt = reservationResponse.ExpiresAt;
        e.IdempotencyKey = reservationResponse.IdempotencyKey;

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