using TicketProcessor.Domain;
using TicketProcessor.Domain.Dto;

namespace TicketProcessor.Application.Interfaces;

public interface IEventTicketTypeRepository
{
    Task AddRangeAsync(IEnumerable<EventTicketTypeDto> items, CancellationToken ct);
    Task<EventTicketTypeDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(Guid eventId, string name, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task<Guid>AddAsync(EventTicketTypeDto ticketType, CancellationToken ct);
    Task IncrementSoldAsync(Guid eventTicketTypeId, int qty, CancellationToken ct);
}