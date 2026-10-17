using BioVic.Domain.Common;
using BioVic.Domain.Enums;

namespace BioVic.Domain.Entities;

public sealed class MaintenanceRecord : AuditableEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public MaintenanceType Type { get; set; }
    public string? WorkOrderNumber { get; set; }
    public DateTimeOffset? ScheduledAtUtc { get; set; }
    public DateTimeOffset PerformedAtUtc { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? Responsible { get; set; }
    public string Activities { get; set; } = string.Empty;
    public string? Findings { get; set; }
    public string? TechnicalDiagnosis { get; set; }
    public string? ActionsTaken { get; set; }
    public string? SparePartsSummary { get; set; }
    public decimal? Cost { get; set; }
    public int? DowntimeMinutes { get; set; }
    public MaintenanceResult Result { get; set; }
    public EquipmentStatus? ResultingStatus { get; set; }
    public DateTimeOffset? NextMaintenanceUtc { get; set; }
    public string? Notes { get; set; }
    public string? SignatureName { get; set; }
}
