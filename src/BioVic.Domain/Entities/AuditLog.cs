using BioVic.Domain.Enums;

namespace BioVic.Domain.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public string TimestampLocal { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string Username { get; set; } = "ANONYMOUS";
    public UserRole? Role { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public Guid? EquipmentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ComputerName { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public string PreviousHash { get; set; } = string.Empty;
    public string RecordHash { get; set; } = string.Empty;
}
