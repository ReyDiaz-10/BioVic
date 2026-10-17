using BioVic.Domain.Common;

namespace BioVic.Domain.Entities;

public sealed class RegulatoryRequirement : SoftDeletableEntity
{
    public string StandardName { get; set; } = string.Empty;
    public string? ArticleOrSection { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset? EffectiveFromUtc { get; set; }
    public string Status { get; set; } = "Vigente";
    public string? ReferenceUrl { get; set; }
    public string? Notes { get; set; }
}
