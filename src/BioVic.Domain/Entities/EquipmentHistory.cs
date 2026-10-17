using BioVic.Domain.Enums;

namespace BioVic.Domain.Entities;

public sealed class EquipmentLocationHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public Guid? PreviousLocationId { get; set; }
    public Guid? NewLocationId { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? DeliveredBy { get; set; }
    public string? ReceivedBy { get; set; }
    public Guid ChangedByUserId { get; set; }
}

public sealed class EquipmentStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public EquipmentStatus PreviousStatus { get; set; }
    public EquipmentStatus NewStatus { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
}

public sealed class EquipmentResponsibleHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EquipmentId { get; set; }
    public string? PreviousResponsible { get; set; }
    public string NewResponsible { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Area { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
    public Guid ChangedByUserId { get; set; }
}
