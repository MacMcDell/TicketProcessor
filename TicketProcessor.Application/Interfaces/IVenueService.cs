using TicketProcessor.Domain.Dto;
using TicketProcessor.Domain.Requests;

namespace TicketProcessor.Application.Interfaces;

public interface IVenueService
{
    Task<VenueDto> CreateVenueAsync(Request.CreateVenueDto request, CancellationToken ct = default);
    Task<VenueDto> UpdateVenueAsync(VenueDto input, CancellationToken ct);
}