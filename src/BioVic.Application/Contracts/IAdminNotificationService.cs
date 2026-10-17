using BioVic.Application.DTOs;

namespace BioVic.Application.Contracts;

public interface IAdminNotificationService
{
    Task<IReadOnlyList<AdminNotificationDto>> ListAsync(RequestContext actor, CancellationToken cancellationToken = default);
    Task SnoozeAsync(Guid notificationId, SnoozeNotificationRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task ResolveAsync(Guid notificationId, RequestContext actor, CancellationToken cancellationToken = default);
    Task CreateMissingLocationAsync(Guid equipmentId, string equipmentName, Guid previousLocationId, string previousLocationName, RequestContext actor, CancellationToken cancellationToken = default);
    Task ResolveMissingLocationAsync(Guid equipmentId, CancellationToken cancellationToken = default);
}
