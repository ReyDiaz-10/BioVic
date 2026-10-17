using BioVic.Application.DTOs;

namespace BioVic.Application.Contracts;

public interface IRecoveryService
{
    Task<RecoverySummaryDto> ListAsync(RequestContext actor, CancellationToken cancellationToken = default);
    Task RestoreEquipmentAsync(Guid id, RestoreDeletedItemRequest request, RequestContext actor,
        CancellationToken cancellationToken = default);
    Task RestoreUserAsync(Guid id, RestoreDeletedItemRequest request, RequestContext actor,
        CancellationToken cancellationToken = default);
    Task RestoreLocationAsync(Guid id, RestoreDeletedItemRequest request, RequestContext actor,
        CancellationToken cancellationToken = default);
}

