using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using BioVic.Domain.Entities;
using BioVic.Domain.Enums;
using BioVic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BioVic.Infrastructure.Services;

public sealed class AdminNotificationService(BioVicDbContext dbContext, IAuditService auditService) : IAdminNotificationService
{
    public async Task<IReadOnlyList<AdminNotificationDto>> ListAsync(RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureMaster(actor);
        await RefreshAutomaticAlertsAsync(actor, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        return await dbContext.AdminNotifications.AsNoTracking()
            .Where(item => item.ResolvedAtUtc == null && (item.SnoozedUntilUtc == null || item.SnoozedUntilUtc <= now))
            .OrderByDescending(item => item.Severity == AlertSeverity.Critical ? 4 :
                item.Severity == AlertSeverity.High ? 3 :
                item.Severity == AlertSeverity.Medium ? 2 :
                item.Severity == AlertSeverity.Low ? 1 : 0)
            .ThenBy(item => item.DueAtUtc).ThenByDescending(item => item.CreatedAtUtc)
            .Select(item => new AdminNotificationDto(item.Id, item.NotificationType, item.Severity, item.Title,
                item.Message, item.EquipmentId, item.LocationId, item.DueAtUtc, item.CreatedAtUtc,
                item.SnoozedUntilUtc, item.ResolvedAtUtc != null))
            .ToListAsync(cancellationToken);
    }

    public async Task SnoozeAsync(Guid notificationId, SnoozeNotificationRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureMaster(actor);
        if (request.UntilUtc <= DateTimeOffset.UtcNow) throw new ValidationException("La fecha para posponer debe ser futura.");
        var notification = await dbContext.AdminNotifications.SingleOrDefaultAsync(item => item.Id == notificationId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el mensaje.");
        notification.SnoozedUntilUtc = request.UntilUtc;
        notification.UpdatedAtUtc = DateTimeOffset.UtcNow;
        notification.UpdatedByUserId = actor.UserId;
        await auditService.AppendAsync(new(actor, "NOTIFICATION_SNOOZED", "AdminNotification", notification.Id.ToString("D"), notification.EquipmentId,
            $"Se pospuso el mensaje '{notification.Title}' hasta {request.UntilUtc:O}."), cancellationToken);
    }

    public async Task ResolveAsync(Guid notificationId, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureMaster(actor);
        var notification = await dbContext.AdminNotifications.SingleOrDefaultAsync(item => item.Id == notificationId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el mensaje.");
        if (notification.SourceKey.StartsWith("MISSING_LOCATION:", StringComparison.Ordinal) && notification.EquipmentId is Guid equipmentId &&
            await dbContext.Equipment.AnyAsync(item => item.Id == equipmentId && item.LocationId == null, cancellationToken))
            throw new ValidationException("Este mensaje solo se resuelve al asignar una ubicación al equipo. Puede posponerlo si lo atenderá después.");
        notification.ResolvedAtUtc = DateTimeOffset.UtcNow;
        notification.UpdatedAtUtc = DateTimeOffset.UtcNow;
        notification.UpdatedByUserId = actor.UserId;
        await auditService.AppendAsync(new(actor, "NOTIFICATION_RESOLVED", "AdminNotification", notification.Id.ToString("D"), notification.EquipmentId,
            $"Se marcó como atendido el mensaje '{notification.Title}'."), cancellationToken);
    }

    public async Task CreateMissingLocationAsync(Guid equipmentId, string equipmentName, Guid previousLocationId,
        string previousLocationName, RequestContext actor, CancellationToken cancellationToken = default)
    {
        var sourceKey = $"MISSING_LOCATION:{equipmentId:D}";
        var existing = await dbContext.AdminNotifications.SingleOrDefaultAsync(item => item.SourceKey == sourceKey, cancellationToken);
        if (existing is not null)
        {
            existing.ResolvedAtUtc = null;
            existing.SnoozedUntilUtc = null;
            existing.LocationId = previousLocationId;
            existing.Title = $"Asignar ubicación a {equipmentName}";
            existing.Message = $"La ubicación '{previousLocationName}' fue eliminada. El equipo quedó sin ubicación y requiere una nueva asignación.";
            return;
        }
        dbContext.AdminNotifications.Add(new AdminNotification
        {
            SourceKey = sourceKey,
            NotificationType = "Ubicación no disponible",
            Severity = AlertSeverity.High,
            Title = $"Asignar ubicación a {equipmentName}",
            Message = $"La ubicación '{previousLocationName}' fue eliminada. El equipo quedó sin ubicación y requiere una nueva asignación.",
            EquipmentId = equipmentId,
            LocationId = previousLocationId,
            CreatedByUserId = actor.UserId
        });
    }

    public async Task ResolveMissingLocationAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var notifications = await dbContext.AdminNotifications
            .Where(item => item.SourceKey == $"MISSING_LOCATION:{equipmentId:D}" && item.ResolvedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var item in notifications) item.ResolvedAtUtc = DateTimeOffset.UtcNow;
    }

    private async Task RefreshAutomaticAlertsAsync(RequestContext actor, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var upcoming = now.AddDays(30);
        var equipment = await dbContext.Equipment.AsNoTracking()
            .Where(item => item.LocationId == null ||
                (item.NextMaintenanceUtc != null && item.NextMaintenanceUtc <= upcoming) ||
                (item.RequiresCalibration && item.NextCalibrationUtc != null && item.NextCalibrationUtc <= upcoming))
            .Select(item => new { item.Id, item.Name, item.LocationId, item.NextMaintenanceUtc, item.NextCalibrationUtc, item.RequiresCalibration })
            .ToListAsync(cancellationToken);

        foreach (var item in equipment)
        {
            if (item.LocationId is null)
                await UpsertAsync($"MISSING_LOCATION:{item.Id:D}", "Ubicación no disponible", AlertSeverity.High,
                    $"Asignar ubicación a {item.Name}", "El equipo no tiene una ubicación activa. Edite su ficha y seleccione una ubicación disponible.", item.Id, null, null, actor, cancellationToken);
            if (item.NextMaintenanceUtc is not null && item.NextMaintenanceUtc <= upcoming)
                await UpsertAsync($"MAINTENANCE:{item.Id:D}:{item.NextMaintenanceUtc.Value.UtcTicks}", "Mantenimiento", item.NextMaintenanceUtc < now ? AlertSeverity.Critical : AlertSeverity.Medium,
                    item.NextMaintenanceUtc < now ? $"Mantenimiento vencido: {item.Name}" : $"Mantenimiento próximo: {item.Name}",
                    $"La fecha programada es {item.NextMaintenanceUtc.Value.ToLocalTime():dd/MM/yyyy}.", item.Id, item.LocationId, item.NextMaintenanceUtc, actor, cancellationToken);
            if (item.RequiresCalibration && item.NextCalibrationUtc is not null && item.NextCalibrationUtc <= upcoming)
                await UpsertAsync($"CALIBRATION:{item.Id:D}:{item.NextCalibrationUtc.Value.UtcTicks}", "Calibración", item.NextCalibrationUtc < now ? AlertSeverity.Critical : AlertSeverity.Medium,
                    item.NextCalibrationUtc < now ? $"Calibración vencida: {item.Name}" : $"Calibración próxima: {item.Name}",
                    $"La fecha programada es {item.NextCalibrationUtc.Value.ToLocalTime():dd/MM/yyyy}.", item.Id, item.LocationId, item.NextCalibrationUtc, actor, cancellationToken);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertAsync(string sourceKey, string type, AlertSeverity severity, string title, string message,
        Guid equipmentId, Guid? locationId, DateTimeOffset? dueAtUtc, RequestContext actor, CancellationToken cancellationToken)
    {
        if (await dbContext.AdminNotifications.AnyAsync(item => item.SourceKey == sourceKey, cancellationToken)) return;
        dbContext.AdminNotifications.Add(new AdminNotification
        {
            SourceKey = sourceKey,
            NotificationType = type,
            Severity = severity,
            Title = title,
            Message = message,
            EquipmentId = equipmentId,
            LocationId = locationId,
            DueAtUtc = dueAtUtc,
            CreatedByUserId = actor.UserId
        });
    }

    private static void EnsureMaster(RequestContext actor)
    {
        if (actor.Role != UserRole.MasterAdministrator) throw new ForbiddenException("Esta acción requiere el rol Administrador Maestro.");
    }
}
