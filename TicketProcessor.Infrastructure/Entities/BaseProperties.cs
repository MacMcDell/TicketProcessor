
using System.ComponentModel.DataAnnotations;

namespace TicketProcessor.Infrastructure;

public class BaseProperties
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid(); 
    
    public DateTimeOffset Created { get; set; } = DateTime.UtcNow;
    
    public string? CreatedBy { get; set; } = "System";
    
    public DateTimeOffset? LastModified { get; set; } = DateTime.UtcNow;
    
    public string? LastModifiedBy { get; set; } = "System";
    
    public bool IsDeleted { get; set; } = false;
}