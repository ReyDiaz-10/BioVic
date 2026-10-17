using BioVic.Domain.Common;
using BioVic.Domain.Enums;

namespace BioVic.Domain.Entities;

public sealed class TechnoVigilanceIncident : AuditableEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Consequence { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public string? ActionsTaken { get; set; }
    public bool WasReportedExternally { get; set; }
    public string? ExternalReportNumber { get; set; }
    public DateTimeOffset? ExternalReportDateUtc { get; set; }
    public string? ReportedEntity { get; set; }
    public string? Conclusions { get; set; }
    public Guid? RelatedMaintenanceId { get; set; }
}
