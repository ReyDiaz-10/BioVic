using BioVic.Domain.Common;
using BioVic.Domain.Enums;

namespace BioVic.Domain.Entities;

public sealed class User : SoftDeletableEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool HasAllLocations { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEndUtc { get; set; }
    public DateTimeOffset? LastLoginAtUtc { get; set; }
    public DateTimeOffset? PasswordChangedAtUtc { get; set; }
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<UserLocationPermission> LocationPermissions { get; set; } = new List<UserLocationPermission>();
}
