namespace BioVic.Application.DTOs;

public sealed record DeletedItemDto(
    Guid Id,
    string ItemType,
    string DisplayName,
    DateTimeOffset DeletedAtUtc,
    string DeletionReason,
    string? Details);

public sealed record RecoverySummaryDto(
    IReadOnlyList<DeletedItemDto> Equipment,
    IReadOnlyList<DeletedItemDto> Users,
    IReadOnlyList<DeletedItemDto> Locations);

public sealed record RestoreDeletedItemRequest(string Reason);

