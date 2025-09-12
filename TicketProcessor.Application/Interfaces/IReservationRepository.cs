using TicketProcessor.Domain.Dto;

namespace TicketProcessor.Application.Interfaces;

public interface IReservationRepository
{
    Task AddAsync(ReservationDto reservation, CancellationToken ct);
    Task<ReservationDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<int> CountActivePendingAsync(Guid eventTicketTypeId, DateTimeOffset now, CancellationToken ct);
    Task<ReservationDto?> GetByIdForUpdateAsync(Guid id, CancellationToken ct); // tracked for updates
    Task UpdateAsync(ReservationDto reservation, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}