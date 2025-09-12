namespace TicketProcessor.Application.Interfaces;

public interface IIdempotencyService
{
    Task<(bool set, Guid? existing)> TrySetAsync(string key, Guid reservationId, TimeSpan ttl, CancellationToken ct = default);
    Task<Guid?> GetAsync(string key);
}