namespace BioVic.Application.DTOs;

public sealed record EquipmentDocumentDto(
    Guid Id,
    Guid EquipmentId,
    string OriginalName,
    string DocumentType,
    string ContentType,
    string? Description,
    long SizeBytes,
    string Sha256,
    int Version,
    bool IsCurrent,
    Guid? SupersedesDocumentId,
    DateTimeOffset? DocumentDateUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool ApprovedForAiKnowledge,
    DateTimeOffset CreatedAtUtc,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    string? DeletionReason);

public sealed record UploadEquipmentDocumentRequest(
    string OriginalName,
    string DocumentType,
    string? Description,
    DateTimeOffset? DocumentDateUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool ApprovedForAiKnowledge,
    Guid? SupersedesDocumentId);

public sealed record EquipmentDocumentDownloadDto(
    string FileName,
    string ContentType,
    byte[] Content,
    string Sha256);

public sealed record ArchiveDocumentRequest(string Reason);
public sealed record RestoreDocumentRequest(string Reason);

public static class EquipmentDocumentTypes
{
    public const string Manual = "Manual";
    public const string TechnicalSheet = "TechnicalSheet";
    public const string CalibrationCertificate = "CalibrationCertificate";
    public const string MaintenanceReport = "MaintenanceReport";
    public const string Photo = "Photo";
    public const string Warranty = "Warranty";
    public const string Invoice = "Invoice";
    public const string Regulatory = "Regulatory";
    public const string Other = "Other";

    public static IReadOnlyList<string> All { get; } =
    [Manual, TechnicalSheet, CalibrationCertificate, MaintenanceReport, Photo, Warranty, Invoice, Regulatory, Other];
}

