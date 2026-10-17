using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using BioVic.Application.Services;
using BioVic.Domain.Entities;
using BioVic.Domain.Enums;
using BioVic.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BioVic.Infrastructure.Services;

public sealed class UserAdminService(
    BioVicDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IAuditService auditService) : IUserAdminService
{
    public async Task<UserSummaryDto> CreateAsync(CreateUserRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureMaster(actor);
        if (request.Role == UserRole.MasterAdministrator)
            throw new ValidationException("BioVic solo admite un Administrador Maestro.");
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            throw new ValidationException("El nombre y los apellidos son obligatorios.");
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length < 4)
            throw new ValidationException("El usuario debe tener al menos 4 caracteres.");
        PasswordPolicy.Validate(request.Password);
        var locationIds = request.LocationIds.Distinct().ToList();
        if (!request.HasAllLocations && locationIds.Count == 0)
            throw new ValidationException("Seleccione al menos una ubicación o active 'Todas las ubicaciones'.");
        if (locationIds.Count > 0 && await dbContext.Locations.CountAsync(location => locationIds.Contains(location.Id), cancellationToken) != locationIds.Count)
            throw new ValidationException("Una o más ubicaciones seleccionadas no existen.");

        var normalized = request.Username.Trim().ToUpperInvariant();
        if (await dbContext.Users.IgnoreQueryFilters().AnyAsync(user => user.NormalizedUsername == normalized, cancellationToken))
            throw new ConflictException($"Ya existe un usuario llamado {request.Username.Trim()}.");

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Username = request.Username.Trim(),
            NormalizedUsername = normalized,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Role = request.Role,
            HasAllLocations = request.HasAllLocations,
            CreatedByUserId = actor.UserId,
            PasswordChangedAtUtc = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        dbContext.Users.Add(user);
        if (!user.HasAllLocations)
            foreach (var locationId in locationIds)
                dbContext.UserLocationPermissions.Add(new() { UserId = user.Id, LocationId = locationId, CreatedByUserId = actor.UserId });
        await auditService.AppendAsync(new(actor, "USER_CREATED", "User", user.Id.ToString("D"), null,
            $"Se creó el usuario {user.Username} con rol {user.Role} y acceso a {(user.HasAllLocations ? "todas las ubicaciones" : locationIds.Count + " ubicación(es)")}."), cancellationToken);
        return Map(user, locationIds);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Users.AsNoTracking().OrderBy(user => user.FirstName).ThenBy(user => user.LastName)
            .Select(user => new UserSummaryDto(user.Id, (user.FirstName + " " + user.LastName).Trim(), user.Username,
                user.Email, user.Role, user.IsEnabled, user.HasAllLocations,
                user.LocationPermissions.Select(permission => permission.LocationId).ToList(), user.CreatedAtUtc, user.LastLoginAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<UserSummaryDto> UpdateAccessAsync(Guid userId, UpdateUserAccessRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureMaster(actor);
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el usuario.");
        if (user.Role == UserRole.MasterAdministrator)
            throw new ValidationException("La cuenta del Administrador Maestro no puede suspenderse ni cambiar de rol.");
        if (request.Role == UserRole.MasterAdministrator)
            throw new ValidationException("BioVic solo admite un Administrador Maestro.");
        var locationIds = request.LocationIds.Distinct().ToList();
        if (!request.HasAllLocations && locationIds.Count == 0)
            throw new ValidationException("Seleccione al menos una ubicación o active 'Todas las ubicaciones'.");
        if (locationIds.Count > 0 && await dbContext.Locations.CountAsync(location => locationIds.Contains(location.Id), cancellationToken) != locationIds.Count)
            throw new ValidationException("Una o más ubicaciones seleccionadas no existen.");

        var previousLocationIds = await dbContext.UserLocationPermissions.Where(item => item.UserId == userId)
            .Select(item => item.LocationId).ToListAsync(cancellationToken);
        var previous = new { user.Role, user.IsEnabled, user.HasAllLocations, LocationIds = previousLocationIds };
        user.Role = request.Role;
        user.IsEnabled = request.IsEnabled;
        user.HasAllLocations = request.HasAllLocations;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        user.UpdatedByUserId = actor.UserId;
        if (!request.IsEnabled)
        {
            var sessions = await dbContext.Sessions.Where(item => item.UserId == userId && item.RevokedAtUtc == null).ToListAsync(cancellationToken);
            foreach (var session in sessions) session.RevokedAtUtc = DateTimeOffset.UtcNow;
        }
        var permissions = await dbContext.UserLocationPermissions.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        dbContext.UserLocationPermissions.RemoveRange(permissions);
        if (!request.HasAllLocations)
            foreach (var locationId in locationIds)
                dbContext.UserLocationPermissions.Add(new() { UserId = userId, LocationId = locationId, CreatedByUserId = actor.UserId });
        await auditService.AppendAsync(new(actor, "USER_ACCESS_UPDATED", "User", user.Id.ToString("D"), null,
            $"Se actualizaron rol y permisos de {user.Username}.", System.Text.Json.JsonSerializer.Serialize(previous),
            System.Text.Json.JsonSerializer.Serialize(new { user.Role, user.IsEnabled, user.HasAllLocations, LocationIds = locationIds })), cancellationToken);
        return Map(user, locationIds);
    }

    public async Task DeleteAsync(Guid userId, DeleteUserRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureMaster(actor);
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ValidationException("Debe indicar el motivo de eliminación.");
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el usuario.");
        if (user.Role == UserRole.MasterAdministrator)
            throw new ValidationException("La cuenta del Administrador Maestro no puede eliminarse.");
        user.IsDeleted = true;
        user.IsEnabled = false;
        user.DeletedAtUtc = DateTimeOffset.UtcNow;
        user.DeletedByUserId = actor.UserId;
        user.DeletionReason = request.Reason.Trim();
        var sessions = await dbContext.Sessions.Where(item => item.UserId == userId && item.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var session in sessions) session.RevokedAtUtc = DateTimeOffset.UtcNow;
        await auditService.AppendAsync(new(actor, "USER_DELETED", "User", user.Id.ToString("D"), null,
            $"Se eliminó lógicamente el usuario {user.Username}. Motivo: {user.DeletionReason}"), cancellationToken);
    }

    private static UserSummaryDto Map(User user, IReadOnlyList<Guid>? locationIds = null) => new(user.Id, $"{user.FirstName} {user.LastName}".Trim(),
        user.Username, user.Email, user.Role, user.IsEnabled, user.HasAllLocations,
        locationIds ?? user.LocationPermissions.Select(item => item.LocationId).ToList(), user.CreatedAtUtc, user.LastLoginAtUtc);

    private static void EnsureMaster(RequestContext actor)
    {
        if (actor.Role != UserRole.MasterAdministrator) throw new ForbiddenException("Esta acción requiere el rol Administrador Maestro.");
    }
}
