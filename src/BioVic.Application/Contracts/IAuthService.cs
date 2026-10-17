using BioVic.Application.DTOs;

namespace BioVic.Application.Contracts;

public interface IAuthService
{
    Task<SetupStatusDto> GetSetupStatusAsync(CancellationToken cancellationToken = default);
    Task<AuthSessionDto> CreateMasterAsync(CreateMasterRequest request, RequestContext context, CancellationToken cancellationToken = default);
    Task<AuthSessionDto> LoginAsync(LoginRequest request, RequestContext context, CancellationToken cancellationToken = default);
    Task<MasterProfileDto> GetMasterProfileAsync(RequestContext context, CancellationToken cancellationToken = default);
    Task<MasterProfileDto> UpdateMasterProfileAsync(UpdateMasterProfileRequest request, RequestContext context, CancellationToken cancellationToken = default);
    Task LogoutAsync(string rawToken, RequestContext context, CancellationToken cancellationToken = default);
    Task<SessionPrincipalDto?> ValidateSessionAsync(string rawToken, CancellationToken cancellationToken = default);
}
