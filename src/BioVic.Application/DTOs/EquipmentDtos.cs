using BioVic.Domain.Enums;

namespace BioVic.Application.DTOs;

public sealed record EquipmentSearchItemDto(
    Guid Id,
    Guid PublicId,
    string InternalCode,
    string Name,
    string? Brand,
    string? Model,
    string? SerialNumber,
    string Location,
    EquipmentStatus Status,
    DateTimeOffset? LastMaintenanceUtc,
    DateTimeOffset? NextMaintenanceUtc,
    Criticality Criticality);

public sealed record EquipmentDetailsDto(
    Guid Id,
    Guid PublicId,
    string InternalCode,
    string Name,
    string? EquipmentType,
    string? Category,
    string? Description,
    string? Manufacturer,
    string? Brand,
    string? Model,
    string? SerialNumber,
    string? InventoryNumber,
    string? AssetCode,
    Guid? LocationId,
    string Location,
    string? ResponsibleName,
    string? ResponsiblePosition,
    EquipmentStatus Status,
    Criticality Criticality,
    bool RequiresCalibration,
    DateTimeOffset? NextMaintenanceUtc,
    DateTimeOffset? NextCalibrationUtc,
    string? PublicOperationalNotes,
    IReadOnlyList<MaintenanceSummaryDto> Maintenances,
    IReadOnlyList<CalibrationSummaryDto> Calibrations,
    IReadOnlyList<FailureSummaryDto> Failures,
    IReadOnlyList<IncidentSummaryDto> Incidents,
    IReadOnlyList<StatusHistoryDto> StatusHistory);

public sealed record CreateEquipmentRequest(
    string InternalCode,
    string Name,
    string? EquipmentType,
    string? Category,
    string? Description,
    string? Manufacturer,
    string? Brand,
    string? Model,
    string? SerialNumber,
    string? InventoryNumber,
    string? AssetCode,
    Guid? LocationId,
    string? ResponsibleName,
    string? ResponsiblePosition,
    EquipmentStatus Status,
    Criticality Criticality,
    bool RequiresCalibration,
    DateTimeOffset? NextMaintenanceUtc,
    DateTimeOffset? NextCalibrationUtc,
    string? PublicOperationalNotes);

public sealed record AddMaintenanceRequest(
    MaintenanceType Type,
    string? WorkOrderNumber,
    DateTimeOffset PerformedAtUtc,
    string PerformedBy,
    string? Responsible,
    string Activities,
    string? Findings,
    string? TechnicalDiagnosis,
    string? ActionsTaken,
    string? SparePartsSummary,
    decimal? Cost,
    int? DowntimeMinutes,
    MaintenanceResult Result,
    EquipmentStatus? ResultingStatus,
    DateTimeOffset? NextMaintenanceUtc,
    string? Notes);

public sealed record UpdateEquipmentRequest(
    string InternalCode, string Name, string? EquipmentType, string? Category, string? Description,
    string? Manufacturer, string? Brand, string? Model, string? SerialNumber, string? InventoryNumber,
    string? AssetCode, Guid? LocationId, string? ResponsibleName, string? ResponsiblePosition,
    EquipmentStatus Status, Criticality Criticality, bool RequiresCalibration,
    DateTimeOffset? NextMaintenanceUtc, DateTimeOffset? NextCalibrationUtc,
    string? PublicOperationalNotes, string ChangeReason);

public sealed record DeleteEquipmentRequest(string Reason);

public sealed record MaintenanceSummaryDto(
    Guid Id,
    MaintenanceType Type,
    DateTimeOffset PerformedAtUtc,
    string PerformedBy,
    string Activities,
    string? Findings,
    string? TechnicalDiagnosis,
    string? ActionsTaken,
    string? SparePartsSummary,
    string? Notes,
    MaintenanceResult Result,
    DateTimeOffset? NextMaintenanceUtc);

public sealed record CalibrationSummaryDto(
    Guid Id,
    DateTimeOffset PerformedAtUtc,
    string Laboratory,
    string? CertificateNumber,
    CalibrationResult Result,
    string? Traceability,
    string? Notes,
    DateTimeOffset? NextCalibrationUtc);

public sealed record AddCalibrationRequest(DateTimeOffset PerformedAtUtc, DateTimeOffset? NextCalibrationUtc,
    string Laboratory, string? CertificateNumber, CalibrationResult Result, string? Traceability, string? Notes);

public sealed record FailureSummaryDto(Guid Id, DateTimeOffset ReportedAtUtc, string Description,
    string ReportedBy, string? Impact, bool EquipmentStopped, string? ImmediateAction,
    string? Diagnosis, string? Solution, DateTimeOffset? ClosedAtUtc);

public sealed record AddFailureRequest(DateTimeOffset ReportedAtUtc, string Description, string ReportedBy,
    string? Service, string? Impact, bool EquipmentStopped, string? ImmediateAction,
    string? Diagnosis, string? Solution, int? DowntimeMinutes, DateTimeOffset? ClosedAtUtc);

public sealed record IncidentSummaryDto(Guid Id, DateTimeOffset OccurredAtUtc, string EventType,
    string Description, string? Consequence, string ReportedBy, IncidentStatus Status,
    string? ActionsTaken, bool WasReportedExternally, string? ExternalReportNumber,
    string? ReportedEntity, string? Conclusions);

public sealed record AddIncidentRequest(DateTimeOffset OccurredAtUtc, string EventType, string Description,
    string? Consequence, string ReportedBy, IncidentStatus Status, string? ActionsTaken,
    bool WasReportedExternally, string? ExternalReportNumber, DateTimeOffset? ExternalReportDateUtc,
    string? ReportedEntity, string? Conclusions);

public sealed record AiQuestionRequest(string Question);
public sealed record AiAnswerDto(string Answer, IReadOnlyList<string> Alerts, string Provider, bool UsedModel);

public sealed record ChangeEquipmentStatusRequest(EquipmentStatus NewStatus, string Reason);

public sealed record StatusHistoryDto(
    Guid Id,
    EquipmentStatus PreviousStatus,
    EquipmentStatus NewStatus,
    DateTimeOffset ChangedAtUtc,
    string Reason);

public sealed record PublicEquipmentDto(
    Guid PublicId,
    string Name,
    string? Brand,
    string? Model,
    string? SerialNumber,
    string InternalCode,
    EquipmentStatus Status,
    string Location,
    DateTimeOffset? LastMaintenanceUtc,
    DateTimeOffset? NextMaintenanceUtc,
    DateTimeOffset? LastCalibrationUtc,
    string? ResponsibleService,
    string? PublicOperationalNotes);

public sealed record QrAccessInfoDto(
    string Url,
    bool IsRemoteAccessReady,
    bool UsesHttps,
    IReadOnlyList<string> Warnings);

public sealed record LocationDto(
    Guid Id,
    string Institution,
    string Site,
    string? Building,
    string? Floor,
    string Service,
    string? Area,
    string? Room,
    string? SpecificLocation,
    string DisplayName);

public sealed record CreateLocationRequest(
    string Institution,
    string Site,
    string? Building,
    string? Floor,
    string Service,
    string? Area,
    string? Room,
    string? SpecificLocation);

public sealed record UpdateLocationRequest(
    string Institution,
    string Site,
    string? Building,
    string? Floor,
    string Service,
    string? Area,
    string? Room,
    string? SpecificLocation,
    string ChangeReason);

public sealed record DeleteLocationRequest(string Reason);

public sealed record AdminNotificationDto(
    Guid Id,
    string NotificationType,
    AlertSeverity Severity,
    string Title,
    string Message,
    Guid? EquipmentId,
    Guid? LocationId,
    DateTimeOffset? DueAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SnoozedUntilUtc,
    bool IsResolved);

public sealed record SnoozeNotificationRequest(DateTimeOffset UntilUtc);
