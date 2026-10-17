using BioVic.Domain.Common;

namespace BioVic.Domain.Entities;

public sealed class FailureRecord : AuditableEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public DateTimeOffset ReportedAtUtc { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReportedBy { get; set; } = string.Empty;
    public string? Service { get; set; }
    public string? Impact { get; set; }
    public bool EquipmentStopped { get; set; }
    public string? ImmediateAction { get; set; }
    public string? Diagnosis { get; set; }
    public string? Solution { get; set; }
    public int? DowntimeMinutes { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
}
