using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using BioVic.Domain.Enums;
using BioVic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BioVic.Infrastructure.Services;

public sealed class RecoveryService(BioVicDbContext db, IAuditService audit) : IRecoveryService
{
    public async Task<RecoverySummaryDto> ListAsync(RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureMaster(actor);
        var equipment = await db.Equipment.IgnoreQueryFilters().AsNoTracking().Where(item => item.IsDeleted)
            .OrderByDescending(item => item.DeletedAtUtc)
            .Select(item => new DeletedItemDto(item.Id, "Equipment", item.Name,
                item.DeletedAtUtc ?? item.CreatedAtUtc, item.DeletionReason ?? "Sin motivo",
                item.InternalCode + " · " + (item.SerialNumber ?? "Sin serial"))).ToListAsync(cancellationToken);
        var users = await db.Users.IgnoreQueryFilters().AsNoTracking().Where(item => item.IsDeleted)
            .OrderByDescending(item => item.DeletedAtUtc)
            .Select(item => new DeletedItemDto(item.Id, "User", (item.FirstName + " " + item.LastName).Trim(),
                item.DeletedAtUtc ?? item.CreatedAtUtc, item.DeletionReason ?? "Sin motivo", item.Username))
            .ToListAsync(cancellationToken);
        var locations = await db.Locations.IgnoreQueryFilters().AsNoTracking().Where(item => item.IsDeleted)
            .OrderByDescending(item => item.DeletedAtUtc)
            .Select(item => new DeletedItemDto(item.Id, "Location", item.Institution + " · " + item.Site,
                item.DeletedAtUtc ?? item.CreatedAtUtc, item.DeletionReason ?? "Sin motivo",
                item.Service + " · " + (item.SpecificLocation ?? ""))).ToListAsync(cancellationToken);
        return new(equipment, users, locations);
    }

    public async Task RestoreEquipmentAsync(Guid id, RestoreDeletedItemRequest request, RequestContext actor,
        CancellationToken cancellationToken = default)
    {
        EnsureMasterAndReason(actor, request.Reason);
        var entity = await db.Equipment.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("No se encontró el equipo.");
        EnsureDeleted(entity.IsDeleted);
        if (await db.Equipment.AnyAsync(item => item.InternalCode == entity.InternalCode, cancellationToken))
            throw new ValidationException("No puede restaurarse porque ya existe otro equipo activo con el mismo código interno.");
        entity.IsDeleted = false; entity.DeletedAtUtc = null; entity.DeletedByUserId = null; entity.DeletionReason = null;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow; entity.UpdatedByUserId = actor.UserId;
        await audit.AppendAsync(new(actor, "EQUIPMENT_RESTORED", "Equipment", id.ToString("D"), id,
            $"Se restauró el equipo {entity.Name}. Motivo: {request.Reason.Trim()}"), cancellationToken);
    }

    public async Task RestoreUserAsync(Guid id, RestoreDeletedItemRequest request, RequestContext actor,
        CancellationToken cancellationToken = default)
    {
        EnsureMasterAndReason(actor, request.Reason);
        var entity = await db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("No se encontró el usuario.");
        EnsureDeleted(entity.IsDeleted);
        if (entity.Role == UserRole.MasterAdministrator)
            throw new ValidationException("La cuenta maestra no puede restaurarse mediante este flujo.");
        if (await db.Users.AnyAsync(item => item.NormalizedUsername == entity.NormalizedUsername, cancellationToken))
            throw new ValidationException("No puede restaurarse porque ya existe otro usuario activo con el mismo nombre de usuario.");
        entity.IsDeleted = false; entity.IsEnabled = true; entity.DeletedAtUtc = null; entity.DeletedByUserId = null;
        entity.DeletionReason = null; entity.UpdatedAtUtc = DateTimeOffset.UtcNow; entity.UpdatedByUserId = actor.UserId;
        await audit.AppendAsync(new(actor, "USER_RESTORED", "User", id.ToString("D"), null,
            $"Se restauró el usuario {entity.Username}. Motivo: {request.Reason.Trim()}"), cancellationToken);
    }

    public async Task RestoreLocationAsync(Guid id, RestoreDeletedItemRequest request, RequestContext actor,
        CancellationToken cancellationToken = default)
    {
        EnsureMasterAndReason(actor, request.Reason);
        var entity = await db.Locations.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("No se encontró la ubicación.");
        EnsureDeleted(entity.IsDeleted);
        entity.IsDeleted = false; entity.DeletedAtUtc = null; entity.DeletedByUserId = null; entity.DeletionReason = null;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow; entity.UpdatedByUserId = actor.UserId;
        await audit.AppendAsync(new(actor, "LOCATION_RESTORED", "Location", id.ToString("D"), null,
            $"Se restauró la ubicación {entity.DisplayName}. Los equipos deben reasignarse manualmente. Motivo: {request.Reason.Trim()}"), cancellationToken);
    }

    private static void EnsureMaster(RequestContext actor)
    {
        if (actor.UserId is null || actor.Role != UserRole.MasterAdministrator)
            throw new ForbiddenException("Esta acción requiere Administrador Maestro.");
    }

    private static void EnsureMasterAndReason(RequestContext actor, string reason)
    {
        EnsureMaster(actor);
        if (string.IsNullOrWhiteSpace(reason)) throw new ValidationException("Indique el motivo de restauración.");
    }

    private static void EnsureDeleted(bool isDeleted)
    {
        if (!isDeleted) throw new ValidationException("El elemento ya está activo.");
    }
}
