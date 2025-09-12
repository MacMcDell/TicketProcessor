using TicketProcessor.Domain;

namespace TicketProcessor.Application.Interfaces;

public interface IReservationRepository
{
    Task AddAsync(ReservationResponseDto reservationResponse, CancellationToken ct);
    Task<ReservationResponseDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<int> CountActivePendingAsync(Guid eventTicketTypeId, DateTimeOffset now, CancellationToken ct);
    Task<ReservationResponseDto?> GetByIdForUpdateAsync(Guid id, CancellationToken ct); // tracked for updates
    Task UpdateAsync(ReservationResponseDto reservationResponse, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}