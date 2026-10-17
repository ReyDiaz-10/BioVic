using BioVic.Application.DTOs;

namespace BioVic.Application.Contracts;

public interface IEquipmentDocumentService
{
    Task<IReadOnlyList<EquipmentDocumentDto>> ListAsync(Guid equipmentId, RequestContext actor,
        bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<EquipmentDocumentDto> UploadAsync(Guid equipmentId, UploadEquipmentDocumentRequest request,
        Stream content, RequestContext actor, CancellationToken cancellationToken = default);
    Task<EquipmentDocumentDownloadDto> DownloadAsync(Guid equipmentId, Guid documentId,
        RequestContext actor, CancellationToken cancellationToken = default);
    Task ArchiveAsync(Guid equipmentId, Guid documentId, ArchiveDocumentRequest request,
        RequestContext actor, CancellationToken cancellationToken = default);
    Task<EquipmentDocumentDto> RestoreAsync(Guid equipmentId, Guid documentId, RestoreDocumentRequest request,
        RequestContext actor, CancellationToken cancellationToken = default);
}

