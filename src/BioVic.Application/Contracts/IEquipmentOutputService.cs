using BioVic.Application.DTOs;

namespace BioVic.Application.Contracts;

public interface IEquipmentOutputService
{
    Task<QrAccessInfoDto> GetQrAccessInfoAsync(Guid equipmentId, RequestContext actor, CancellationToken cancellationToken = default);
    Task<byte[]> CreateQrPngAsync(Guid equipmentId, RequestContext actor, CancellationToken cancellationToken = default);
    Task<byte[]> CreateLifeSheetPdfAsync(Guid equipmentId, RequestContext actor, CancellationToken cancellationToken = default);
    Task<byte[]> CreatePublicLifeSheetPdfAsync(Guid publicId, RequestContext actor, CancellationToken cancellationToken = default);
    Task<AiAnswerDto> AskAsync(Guid equipmentId, AiQuestionRequest request, RequestContext actor, CancellationToken cancellationToken = default);
}
