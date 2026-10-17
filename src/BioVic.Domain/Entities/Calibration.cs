using BioVic.Domain.Common;
using BioVic.Domain.Enums;

namespace BioVic.Domain.Entities;

public sealed class Calibration : AuditableEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public DateTimeOffset PerformedAtUtc { get; set; }
    public DateTimeOffset? NextCalibrationUtc { get; set; }
    public string Laboratory { get; set; } = string.Empty;
    public string? CertificateNumber { get; set; }
    public CalibrationResult Result { get; set; }
    public string? Traceability { get; set; }
    public string? Notes { get; set; }
    public Guid? CertificateDocumentId { get; set; }
}
