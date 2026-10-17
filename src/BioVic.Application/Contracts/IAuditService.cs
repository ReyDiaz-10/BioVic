using BioVic.Application.DTOs;

namespace BioVic.Application.Contracts;

public interface IAuditService
{
    Task<long> AppendAsync(AuditEntryRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLogDto>> QueryAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AuditIntegrityDto> VerifyIntegrityAsync(CancellationToken cancellationToken = default);
}
