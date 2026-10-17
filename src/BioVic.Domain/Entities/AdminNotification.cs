using BioVic.Domain.Common;
using BioVic.Domain.Enums;

namespace BioVic.Domain.Entities;

public sealed class AdminNotification : AuditableEntity
{
    public string SourceKey { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? EquipmentId { get; set; }
    public Guid? LocationId { get; set; }
    public DateTimeOffset? DueAtUtc { get; set; }
    public DateTimeOffset? SnoozedUntilUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
}
