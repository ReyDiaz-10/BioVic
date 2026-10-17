using BioVic.Domain.Enums;

namespace BioVic.Application.DTOs;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    string? Suggestion = null);

public sealed record RequestContext(
    Guid? UserId,
    string Username,
    UserRole? Role,
    string SessionId,
    string ComputerName,
    string IpAddress,
    string ApplicationVersion)
{
    public static RequestContext Anonymous(
        string computerName = "UNKNOWN",
        string ipAddress = "UNKNOWN",
        string applicationVersion = "UNKNOWN") =>
        new(null, "ANONYMOUS", null, "ANONYMOUS", computerName, ipAddress, applicationVersion);
}

public sealed record SetupStatusDto(bool RequiresMasterAdministrator);

public sealed record DashboardDto(
    int TotalEquipment,
    int Operational,
    int OutOfService,
    int InMaintenance,
    int OverdueMaintenance,
    int UpcomingMaintenance,
    int OverdueCalibrations,
    int OpenIncidents,
    decimal OperationalPercentage);
