using BioVic.Domain.Enums;

namespace BioVic.Application.DTOs;

public sealed record AuditEntryRequest(
    RequestContext Actor,
    string Action,
    string EntityType,
    string? EntityId,
    Guid? EquipmentId,
    string Description,
    string? OldValue = null,
    string? NewValue = null);

public sealed record AuditLogDto(
    long Id,
    DateTimeOffset TimestampUtc,
    string TimestampLocal,
    Guid? UserId,
    string Username,
    UserRole? Role,
    string Action,
    string EntityType,
    string? EntityId,
    Guid? EquipmentId,
    string Description,
    string? OldValue,
    string? NewValue,
    string ComputerName,
    string IpAddress,
    string ApplicationVersion,
    string PreviousHash,
    string RecordHash);

public sealed record AuditIntegrityDto(bool IsValid, long RecordsChecked, long? FirstInvalidRecordId, string Message);
