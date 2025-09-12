using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;

namespace TicketProcessor.Application.Services;

public class VenueService : IVenueService
{
    private readonly IVenueRepository _venues;
    
    private readonly IUnitOfWork _uow;

    public VenueService(
        IVenueRepository venues,
        IUnitOfWork uow)
    {
        _venues = venues;
        _uow = uow;
        
    }
    
    public async Task<VenueDto> CreateVenueAsync(VenueDto request, CancellationToken ct = default)
    {
        // Ensure venue (existing or create new)
      
            if (await _venues.FindByNameAsync(request.Name, ct))
                throw new InvalidOperationException($"Venue already exists!");
            
        var venueId = await _venues.AddAsync(new VenueDto{
           Id =  Guid.NewGuid(), 
           Name = request.Name!.Trim(), 
           Capacity = request.Capacity}, ct);
       
        await _uow.SaveChangesAsync(ct);
        var venue = await _venues.GetAsync(venueId, ct);
        return venue ?? throw new InvalidOperationException($"Venue {venueId} not found.");

    }

    public async Task<VenueDto> UpdateVenueAsync(VenueDto input, CancellationToken ct)
    {
        var existing = await _venues.GetAsync(input.Id!.Value, ct);
        if(existing is null)
            throw new InvalidOperationException($"Venue {input.Id} not found.");
        
        await _venues.UpdateAsync(input, ct); 
        await _uow.SaveChangesAsync(ct);
        return input;
    }

    public async Task<List<VenueDto>> GetVenuesAsync(CancellationToken ct) => await _venues.GetVenuesAsync(ct);
    public async Task DeleteVenueAsync(Guid id, CancellationToken ct)
    {
        await _venues.DeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);    
    }
}