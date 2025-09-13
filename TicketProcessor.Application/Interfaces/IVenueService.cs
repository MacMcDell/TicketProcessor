using TicketProcessor.Domain;

namespace TicketProcessor.Application.Interfaces;

public interface IVenueService
{
    Task<VenueDto> CreateVenueAsync(VenueDto request, CancellationToken ct = default);
    Task<VenueDto> UpdateVenueAsync(VenueDto input, CancellationToken ct);
    Task<List<VenueDto>> GetVenuesAsync(CancellationToken ct);

    Task DeleteVenueAsync(Guid id, CancellationToken ct);
}