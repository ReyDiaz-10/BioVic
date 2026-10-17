using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Domain.Enums;
using BioVic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BioVic.Infrastructure.Services;

public sealed class DashboardService(BioVicDbContext dbContext) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var upcoming = now.AddDays(30);
        var query = dbContext.Equipment.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var operational = await query.CountAsync(equipment => equipment.Status == EquipmentStatus.Operational ||
            equipment.Status == EquipmentStatus.OperationalWithObservations, cancellationToken);
        var outOfService = await query.CountAsync(equipment => equipment.Status == EquipmentStatus.OutOfService, cancellationToken);
        var inMaintenance = await query.CountAsync(equipment => equipment.Status == EquipmentStatus.InMaintenance, cancellationToken);
        var overdueMaintenance = await query.CountAsync(equipment => equipment.NextMaintenanceUtc != null && equipment.NextMaintenanceUtc < now, cancellationToken);
        var upcomingMaintenance = await query.CountAsync(equipment => equipment.NextMaintenanceUtc != null &&
            equipment.NextMaintenanceUtc >= now && equipment.NextMaintenanceUtc <= upcoming, cancellationToken);
        var overdueCalibrations = await query.CountAsync(equipment => equipment.RequiresCalibration &&
            equipment.NextCalibrationUtc != null && equipment.NextCalibrationUtc < now, cancellationToken);
        var openIncidents = await dbContext.TechnoVigilanceIncidents.AsNoTracking()
            .CountAsync(incident => incident.Status != IncidentStatus.Closed, cancellationToken);
        var percentage = total == 0 ? 0m : Math.Round(operational * 100m / total, 1);
        return new(total, operational, outOfService, inMaintenance, overdueMaintenance, upcomingMaintenance,
            overdueCalibrations, openIncidents, percentage);
    }
}
