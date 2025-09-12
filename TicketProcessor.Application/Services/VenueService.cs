using AutoMapper;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain.Dto;
using TicketProcessor.Domain.Requests;

namespace TicketProcessor.Application.Services;

public class VenueService : IVenueService
{
    private readonly IVenueRepository _venues;
    
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public VenueService(
        IVenueRepository venues,
        IUnitOfWork uow,
        IMapper mapper)
    {
        _venues = venues;
      
        _uow = uow;
        _mapper = mapper;
    }
    
    public async Task<VenueDto> CreateVenueAsync(Request.CreateVenueDto request, CancellationToken ct = default)
    {
        // Ensure venue (existing or create new)
      
            if (await _venues.FindByNameAsync(request.Name, ct))
                throw new InvalidOperationException($"Venue already exists!");
            
        var venueId = await _venues.AddAsync(new VenueDto(Guid.NewGuid(), request.Name!.Trim(), request.Capacity), ct);
       
        // One commit
        await _uow.SaveChangesAsync(ct);
        var venue = await _venues.GetAsync(venueId, ct);
        return venue ?? throw new InvalidOperationException($"Venue {venueId} not found.");

    }

    public async Task<VenueDto> UpdateVenueAsync(VenueDto input, CancellationToken ct)
    {
        var existing = await _venues.GetAsync(input.Id, ct);
        if(existing is null)
            throw new InvalidOperationException($"Venue {input.Id} not found.");
        
        
        await _venues.UpdateAsync(input, ct); 
        await _uow.SaveChangesAsync(ct);
        return input;
    }
}