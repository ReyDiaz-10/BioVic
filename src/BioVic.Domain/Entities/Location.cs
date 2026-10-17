using BioVic.Domain.Common;

namespace BioVic.Domain.Entities;

public sealed class Location : SoftDeletableEntity
{
    public string Institution { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string Service { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? Room { get; set; }
    public string? SpecificLocation { get; set; }
    public string SearchTextNormalized { get; set; } = string.Empty;
    public ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
    public ICollection<UserLocationPermission> UserPermissions { get; set; } = new List<UserLocationPermission>();

    public string DisplayName => string.Join(" · ", new[] { Institution, Site, Service, Area, Room, SpecificLocation }
        .Where(value => !string.IsNullOrWhiteSpace(value)));
}
