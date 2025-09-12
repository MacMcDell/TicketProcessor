using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;
using TicketProcessor.Domain.Dto;

namespace TicketProcessor.Infrastructure.Repositories;

public sealed class VenueRepository : IVenueRepository
{
    private readonly TicketingDbContext _db;
    private readonly IMapper _mapper;
    public VenueRepository(TicketingDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        => _db.Venues.AnyAsync(v => v.Id == id, ct);

    public async Task<VenueDto?> GetAsync(Guid id, CancellationToken ct)
        => await _db.Venues
            .Where(v => v.Id == id)
            .ProjectTo<VenueDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

    public async Task<Guid> AddAsync(VenueDto venue, CancellationToken ct)
    {
        var entity = _mapper.Map<Venue>(venue); // map DTO -> EF entity
        entity.Id = venue.Id == Guid.Empty ? Guid.NewGuid() : venue.Id;
        await _db.Venues.AddAsync(entity, ct);
        return entity.Id;
    }

    public async Task UpdateAsync(VenueDto existingVenue, CancellationToken ct)
    {
        var entity = await _db.Venues.FirstOrDefaultAsync(x => x.Id == existingVenue.Id, ct)
                     ?? throw new InvalidOperationException("Event not found.");
        
        entity.Capacity = existingVenue.Capacity;
        entity.Name = existingVenue.Name;
        _db.Attach(entity);
        _db.Entry(entity).Property(x => x.Capacity).IsModified = true;
        _db.Entry(entity).Property(x => x.Name).IsModified = true;
    }

    public async Task<bool> FindByNameAsync(string requestName, CancellationToken ct)
    {
        var result = await _db.Venues.AnyAsync(v => v.Name == requestName, ct);
        return result;
    }
}