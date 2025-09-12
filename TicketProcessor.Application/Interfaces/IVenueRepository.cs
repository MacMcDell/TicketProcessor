using TicketProcessor.Domain;

namespace TicketProcessor.Application.Interfaces;

public interface IVenueRepository : IBasicLookup
{
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task<VenueDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> AddAsync(VenueDto venue, CancellationToken ct); // returns new
    Task UpdateAsync(VenueDto existingVenue, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<List<VenueDto>> GetVenuesAsync(CancellationToken ct);

}

public interface IBasicLookup
{
    Task<bool> FindByNameAsync(string requestName, CancellationToken ct);
}