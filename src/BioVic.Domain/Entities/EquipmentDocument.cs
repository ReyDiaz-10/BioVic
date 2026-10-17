using BioVic.Domain.Common;

namespace BioVic.Domain.Entities;

public sealed class EquipmentDocument : SoftDeletableEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public string OriginalName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string? Description { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int Version { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public Guid? SupersedesDocumentId { get; set; }
    public DateTimeOffset? DocumentDateUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public bool ApprovedForAiKnowledge { get; set; }
}
