using BioVic.Application.DTOs;

namespace BioVic.Application.Contracts;

public interface IEquipmentService
{
    Task<PagedResult<EquipmentSearchItemDto>> SearchAsync(string? query, int page, int pageSize, RequestContext actor, CancellationToken cancellationToken = default);
    Task<EquipmentDetailsDto> GetAsync(Guid id, RequestContext actor, CancellationToken cancellationToken = default);
    Task<EquipmentDetailsDto> GetByPublicIdAsync(Guid publicId, RequestContext actor, CancellationToken cancellationToken = default);
    Task<PublicEquipmentDto> GetPublicAsync(Guid publicId, RequestContext actor, CancellationToken cancellationToken = default);
    Task<EquipmentDetailsDto> CreateAsync(CreateEquipmentRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task<EquipmentDetailsDto> UpdateAsync(Guid id, UpdateEquipmentRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, DeleteEquipmentRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task<MaintenanceSummaryDto> AddMaintenanceAsync(Guid equipmentId, AddMaintenanceRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task<CalibrationSummaryDto> AddCalibrationAsync(Guid equipmentId, AddCalibrationRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task<FailureSummaryDto> AddFailureAsync(Guid equipmentId, AddFailureRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task<IncidentSummaryDto> AddIncidentAsync(Guid equipmentId, AddIncidentRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(Guid equipmentId, ChangeEquipmentStatusRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationDto>> GetLocationsAsync(RequestContext actor, CancellationToken cancellationToken = default);
    Task<LocationDto> CreateLocationAsync(CreateLocationRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task<LocationDto> UpdateLocationAsync(Guid id, UpdateLocationRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task DeleteLocationAsync(Guid id, DeleteLocationRequest request, RequestContext actor, CancellationToken cancellationToken = default);
}
