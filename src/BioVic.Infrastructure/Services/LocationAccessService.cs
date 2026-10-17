using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using BioVic.Domain.Enums;
using BioVic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BioVic.Infrastructure.Services;

public sealed record LocationAccessScope(bool HasAllLocations, IReadOnlyList<Guid> LocationIds);

public sealed class LocationAccessService(BioVicDbContext dbContext)
{
    public async Task<LocationAccessScope> GetScopeAsync(RequestContext actor, CancellationToken cancellationToken = default)
    {
        if (actor.UserId is null) throw new AuthenticationException("Debe iniciar sesión para consultar información de BioVic.");
        if (actor.Role == UserRole.MasterAdministrator) return new(true, []);

        var user = await dbContext.Users.AsNoTracking()
            .Where(item => item.Id == actor.UserId && item.IsEnabled)
            .Select(item => new
            {
                item.HasAllLocations,
                LocationIds = item.LocationPermissions.Select(permission => permission.LocationId).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AuthenticationException("La cuenta no está disponible.");

        return new(user.HasAllLocations, user.LocationIds);
    }

    public async Task EnsureEquipmentAccessAsync(Guid equipmentId, RequestContext actor, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(actor, cancellationToken);
        if (scope.HasAllLocations) return;

        var locationId = await dbContext.Equipment.AsNoTracking()
            .Where(item => item.Id == equipmentId)
            .Select(item => item.LocationId)
            .SingleOrDefaultAsync(cancellationToken);
        if (locationId is null || !scope.LocationIds.Contains(locationId.Value))
            throw new ForbiddenException("No tiene permiso para consultar equipos de esta ubicación.");
    }

    public async Task EnsureLocationAccessAsync(Guid locationId, RequestContext actor, CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(actor, cancellationToken);
        if (scope.HasAllLocations) return;
        if (!scope.LocationIds.Contains(locationId))
            throw new ForbiddenException("No tiene permiso para utilizar esta ubicación.");
    }
}
