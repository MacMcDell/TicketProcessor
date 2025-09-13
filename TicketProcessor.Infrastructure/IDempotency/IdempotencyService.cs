using StackExchange.Redis;
using TicketProcessor.Application.Interfaces;

namespace TicketProcessor.Infrastructure.IDempotency;

public class IdempotencyService : IIdempotencyService
{
    private readonly IConnectionMultiplexer _mux;
    private static string IdempotencyKey(string key) => $"idempotency:{key}";

    public IdempotencyService(IConnectionMultiplexer mux)
    {
        _mux = mux;
    }

    public async Task<(bool set, Guid? existing)> TrySetAsync(string key, Guid reservationId, TimeSpan ttl,
        CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        var k = IdempotencyKey(key);
        var ok = await db.StringSetAsync(k, reservationId.ToString("D"), ttl, when: When.NotExists);
        if (ok) return (true, null);

        var val = await db.StringGetAsync(k);
        if (val.HasValue && Guid.TryParse(val.ToString(), out var existing))
            return (false, existing);

        return (false, null);
    }

    public async Task<Guid?> GetAsync(string key)
    {
        var db = _mux.GetDatabase();
        var val = await db.StringGetAsync(IdempotencyKey(key));
        return val.HasValue && Guid.TryParse(val.ToString(), out var id) ? id : null;
    }
}