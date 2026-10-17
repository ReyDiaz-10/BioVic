using BioVic.Application.DTOs;

namespace BioVic.Application.Contracts;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
}
