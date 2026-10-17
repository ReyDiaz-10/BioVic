using BioVic.Domain.Common;

namespace BioVic.Domain.Entities;

public sealed class UserLocationPermission : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;
}
