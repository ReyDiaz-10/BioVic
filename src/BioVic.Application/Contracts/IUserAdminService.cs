using BioVic.Application.DTOs;

namespace BioVic.Application.Contracts;

public interface IUserAdminService
{
    Task<UserSummaryDto> CreateAsync(CreateUserRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<UserSummaryDto> UpdateAccessAsync(Guid userId, UpdateUserAccessRequest request, RequestContext actor, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, DeleteUserRequest request, RequestContext actor, CancellationToken cancellationToken = default);
}
