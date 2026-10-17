using System.Text.Json;
using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using BioVic.Application.Services;
using BioVic.Domain.Entities;
using BioVic.Domain.Enums;
using BioVic.Infrastructure.Data;
using BioVic.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;

namespace BioVic.Infrastructure.Services;

public sealed class EquipmentService(
    BioVicDbContext dbContext,
    IAuditService auditService,
    BioVicOptions options,
    LocationAccessService locationAccess,
    IAdminNotificationService notificationService) : IEquipmentService
{
    public async Task<PagedResult<EquipmentSearchItemDto>> SearchAsync(
        string? query, int page, int pageSize, RequestContext actor, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var scope = await locationAccess.GetScopeAsync(actor, cancellationToken);
        var normalized = TextNormalizer.Normalize(query);
        IQueryable<Equipment> equipmentQuery = dbContext.Equipment.AsNoTracking().Include(equipment => equipment.Location).Include(equipment => equipment.Maintenances);
        if (!scope.HasAllLocations)
            equipmentQuery = equipmentQuery.Where(equipment => equipment.LocationId != null && scope.LocationIds.Contains(equipment.LocationId.Value));
        List<Equipment> entities;
        int total;
        string? suggestion = null;

        if (normalized.Length == 0)
        {
            total = await equipmentQuery.CountAsync(cancellationToken);
            entities = await equipmentQuery.OrderBy(equipment => equipment.Name)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        }
        else
        {
            var raw = query!.Trim();
            var direct = equipmentQuery.Where(equipment => equipment.SearchTextNormalized.Contains(normalized));
            total = await direct.CountAsync(cancellationToken);
            entities = await direct
                .OrderByDescending(equipment => equipment.SerialNumber == raw)
                .ThenByDescending(equipment => equipment.SerialNumber != null && equipment.SerialNumber.StartsWith(raw))
                .ThenBy(equipment => equipment.Name)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            if (total == 0 && normalized.Length >= 3)
            {
                var candidates = await equipmentQuery.OrderBy(equipment => equipment.Name).Take(500).ToListAsync(cancellationToken);
                var ranked = candidates
                    .Select(equipment => new { Equipment = equipment, Score = Score(equipment, normalized) })
                    .OrderByDescending(item => item.Score)
                    .ThenBy(item => item.Equipment.Name)
                    .ToList();
                var best = ranked.FirstOrDefault();
                if (best is not null && best.Score >= 0.55d) suggestion = best.Equipment.Name;
                var matches = ranked.Where(item => item.Score >= 0.52d).ToList();
                total = matches.Count;
                entities = matches.Skip((page - 1) * pageSize).Take(pageSize).Select(item => item.Equipment).ToList();
            }
        }

        await auditService.AppendAsync(new(actor, "EQUIPMENT_SEARCH", "Equipment", null, null,
            $"Búsqueda de equipos: '{query?.Trim() ?? string.Empty}'.", null,
            JsonSerializer.Serialize(new { Results = total, Suggestion = suggestion })), cancellationToken);
        return new(entities.Select(MapSearch).ToList(), page, pageSize, total, suggestion);
    }

    public async Task<EquipmentDetailsDto> GetAsync(Guid id, RequestContext actor, CancellationToken cancellationToken = default)
    {
        await locationAccess.EnsureEquipmentAccessAsync(id, actor, cancellationToken);
        var equipment = await GetEntityAsync(id, cancellationToken);
        await auditService.AppendAsync(new(actor, "EQUIPMENT_VIEWED", "Equipment", id.ToString("D"), id,
            $"Se consultó la hoja de vida de {equipment.Name}."), cancellationToken);
        return MapDetails(equipment);
    }

    public async Task<EquipmentDetailsDto> GetByPublicIdAsync(Guid publicId, RequestContext actor, CancellationToken cancellationToken = default)
    {
        var equipmentId = await dbContext.Equipment.AsNoTracking()
            .Where(item => item.PublicId == publicId)
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No se encontró el equipo solicitado.");
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        var equipment = await GetEntityAsync(equipmentId, cancellationToken);
        await auditService.AppendAsync(new(actor, "QR_VIEWED", "Equipment", equipment.Id.ToString("D"), equipment.Id,
            $"Consulta autenticada por QR de {equipment.Name}."), cancellationToken);
        return MapDetails(equipment);
    }

    public async Task<PublicEquipmentDto> GetPublicAsync(Guid publicId, RequestContext actor, CancellationToken cancellationToken = default)
    {
        var equipment = await dbContext.Equipment.AsNoTracking()
            .Include(item => item.Location).Include(item => item.Maintenances).Include(item => item.Calibrations)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.PublicId == publicId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el equipo solicitado.");
        await locationAccess.EnsureEquipmentAccessAsync(equipment.Id, actor, cancellationToken);
        await auditService.AppendAsync(new(actor, "QR_VIEWED", "Equipment", equipment.Id.ToString("D"), equipment.Id,
            $"Consulta autenticada por QR de {equipment.Name}."), cancellationToken);
        return new(equipment.PublicId, equipment.Name, equipment.Brand, equipment.Model, equipment.SerialNumber,
            equipment.InternalCode, equipment.Status, LocationName(equipment.Location),
            equipment.Maintenances.OrderByDescending(record => record.PerformedAtUtc).Select(record => (DateTimeOffset?)record.PerformedAtUtc).FirstOrDefault(),
            equipment.NextMaintenanceUtc,
            equipment.Calibrations.OrderByDescending(record => record.PerformedAtUtc).Select(record => (DateTimeOffset?)record.PerformedAtUtc).FirstOrDefault(),
            equipment.Location?.Service, equipment.PublicOperationalNotes);
    }

    public async Task<EquipmentDetailsDto> CreateAsync(CreateEquipmentRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureEditor(actor);
        ValidateCreate(request);
        var code = request.InternalCode.Trim();
        if (await dbContext.Equipment.IgnoreQueryFilters().AnyAsync(equipment => equipment.InternalCode == code, cancellationToken))
            throw new ConflictException($"Ya existe un equipo con el código {code}.");
        var serial = EmptyToNull(request.SerialNumber);
        if (options.SerialMustBeUnique && serial is not null &&
            await dbContext.Equipment.IgnoreQueryFilters().AnyAsync(equipment => equipment.SerialNumber == serial, cancellationToken))
            throw new ConflictException($"Ya existe un equipo con el serial {serial}.");
        var location = await dbContext.Locations.SingleOrDefaultAsync(item => item.Id == request.LocationId, cancellationToken)
            ?? throw new ValidationException("La ubicación seleccionada no existe o no está disponible.");
        await locationAccess.EnsureLocationAccessAsync(location.Id, actor, cancellationToken);

        var equipment = new Equipment
        {
            InternalCode = code,
            Name = request.Name.Trim(),
            EquipmentType = EmptyToNull(request.EquipmentType),
            Category = EmptyToNull(request.Category),
            Description = EmptyToNull(request.Description),
            Manufacturer = EmptyToNull(request.Manufacturer),
            Brand = EmptyToNull(request.Brand),
            Model = EmptyToNull(request.Model),
            SerialNumber = serial,
            InventoryNumber = EmptyToNull(request.InventoryNumber),
            AssetCode = EmptyToNull(request.AssetCode),
            LocationId = location.Id,
            ResponsibleName = EmptyToNull(request.ResponsibleName),
            ResponsiblePosition = EmptyToNull(request.ResponsiblePosition),
            Status = request.Status,
            Criticality = request.Criticality,
            RequiresCalibration = request.RequiresCalibration,
            NextMaintenanceUtc = request.NextMaintenanceUtc,
            NextCalibrationUtc = request.NextCalibrationUtc,
            PublicOperationalNotes = EmptyToNull(request.PublicOperationalNotes),
            CreatedByUserId = actor.UserId
        };
        equipment.SearchTextNormalized = BuildSearchText(equipment, location);
        dbContext.Equipment.Add(equipment);
        dbContext.EquipmentStatusHistory.Add(new()
        {
            EquipmentId = equipment.Id,
            PreviousStatus = equipment.Status,
            NewStatus = equipment.Status,
            ChangedAtUtc = DateTimeOffset.UtcNow,
            Reason = "Estado inicial del equipo.",
            ChangedByUserId = actor.UserId!.Value
        });
        await auditService.AppendAsync(new(actor, "EQUIPMENT_CREATED", "Equipment", equipment.Id.ToString("D"), equipment.Id,
            $"Se creó el equipo {equipment.Name} ({equipment.InternalCode}).", null,
            JsonSerializer.Serialize(new { equipment.InternalCode, equipment.Name, equipment.SerialNumber, equipment.Status })), cancellationToken);
        return await GetDetailsWithoutAuditAsync(equipment.Id, cancellationToken);
    }

    public async Task<MaintenanceSummaryDto> AddMaintenanceAsync(
        Guid equipmentId, AddMaintenanceRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureEditor(actor);
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.PerformedBy)) throw new ValidationException("Debe indicar quién realizó el mantenimiento.");
        if (string.IsNullOrWhiteSpace(request.Activities)) throw new ValidationException("La descripción de actividades es obligatoria.");
        if (request.PerformedAtUtc > DateTimeOffset.UtcNow.AddDays(1)) throw new ValidationException("La fecha de mantenimiento no puede estar en el futuro.");
        if (request.NextMaintenanceUtc is not null && request.NextMaintenanceUtc <= request.PerformedAtUtc)
            throw new ValidationException("El próximo mantenimiento debe ser posterior al mantenimiento realizado.");

        var equipment = await dbContext.Equipment.SingleOrDefaultAsync(item => item.Id == equipmentId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el equipo.");
        var previousStatus = equipment.Status;
        var record = new MaintenanceRecord
        {
            EquipmentId = equipmentId,
            Type = request.Type,
            WorkOrderNumber = EmptyToNull(request.WorkOrderNumber),
            PerformedAtUtc = request.PerformedAtUtc,
            PerformedBy = request.PerformedBy.Trim(),
            Responsible = EmptyToNull(request.Responsible),
            Activities = request.Activities.Trim(),
            Findings = EmptyToNull(request.Findings),
            TechnicalDiagnosis = EmptyToNull(request.TechnicalDiagnosis),
            ActionsTaken = EmptyToNull(request.ActionsTaken),
            SparePartsSummary = EmptyToNull(request.SparePartsSummary),
            Cost = request.Cost,
            DowntimeMinutes = request.DowntimeMinutes,
            Result = request.Result,
            ResultingStatus = request.ResultingStatus,
            NextMaintenanceUtc = request.NextMaintenanceUtc,
            Notes = EmptyToNull(request.Notes),
            CreatedByUserId = actor.UserId
        };
        dbContext.MaintenanceRecords.Add(record);
        equipment.NextMaintenanceUtc = request.NextMaintenanceUtc;
        equipment.UpdatedAtUtc = DateTimeOffset.UtcNow;
        equipment.UpdatedByUserId = actor.UserId;
        if (request.ResultingStatus is not null && request.ResultingStatus != previousStatus)
        {
            equipment.Status = request.ResultingStatus.Value;
            dbContext.EquipmentStatusHistory.Add(new()
            {
                EquipmentId = equipment.Id,
                PreviousStatus = previousStatus,
                NewStatus = equipment.Status,
                ChangedAtUtc = DateTimeOffset.UtcNow,
                Reason = $"Resultado del mantenimiento {request.WorkOrderNumber ?? record.Id.ToString("D")}",
                ChangedByUserId = actor.UserId!.Value
            });
        }
        await auditService.AppendAsync(new(actor, "MAINTENANCE_ADDED", "MaintenanceRecord", record.Id.ToString("D"), equipmentId,
            $"Se agregó mantenimiento {record.Type} a {equipment.Name}.", null,
            JsonSerializer.Serialize(new { record.Type, record.PerformedAtUtc, record.Result, record.NextMaintenanceUtc })), cancellationToken);
        return MapMaintenance(record);
    }

    public async Task<EquipmentDetailsDto> UpdateAsync(Guid id, UpdateEquipmentRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        if (actor.Role != UserRole.MasterAdministrator) throw new ForbiddenException("Solo el Administrador Maestro puede editar la ficha maestra de un equipo.");
        if (string.IsNullOrWhiteSpace(request.ChangeReason)) throw new ValidationException("Debe indicar el motivo del cambio.");
        if (string.IsNullOrWhiteSpace(request.InternalCode) || string.IsNullOrWhiteSpace(request.Name)) throw new ValidationException("Código y nombre son obligatorios.");
        var equipment = await dbContext.Equipment.Include(item => item.Location).SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("No se encontró el equipo.");
        if (await dbContext.Equipment.AnyAsync(item => item.Id != id && item.InternalCode == request.InternalCode.Trim(), cancellationToken))
            throw new ConflictException("Ya existe otro equipo con ese código.");
        var oldValue = JsonSerializer.Serialize(new { equipment.InternalCode, equipment.Name, equipment.Model, equipment.SerialNumber, equipment.Status, equipment.Criticality, equipment.LocationId, equipment.ResponsibleName, equipment.ResponsiblePosition, equipment.NextMaintenanceUtc, equipment.NextCalibrationUtc });
        var oldStatus = equipment.Status;
        var oldLocationId = equipment.LocationId;
        var oldResponsibleName = equipment.ResponsibleName;
        var oldResponsiblePosition = equipment.ResponsiblePosition;
        equipment.InternalCode = request.InternalCode.Trim(); equipment.Name = request.Name.Trim();
        equipment.EquipmentType = EmptyToNull(request.EquipmentType); equipment.Category = EmptyToNull(request.Category);
        equipment.Description = EmptyToNull(request.Description); equipment.Manufacturer = EmptyToNull(request.Manufacturer);
        equipment.Brand = EmptyToNull(request.Brand); equipment.Model = EmptyToNull(request.Model);
        equipment.SerialNumber = EmptyToNull(request.SerialNumber); equipment.InventoryNumber = EmptyToNull(request.InventoryNumber);
        equipment.AssetCode = EmptyToNull(request.AssetCode);
        equipment.ResponsibleName = EmptyToNull(request.ResponsibleName); equipment.ResponsiblePosition = EmptyToNull(request.ResponsiblePosition);
        equipment.Status = request.Status; equipment.Criticality = request.Criticality; equipment.RequiresCalibration = request.RequiresCalibration;
        equipment.NextMaintenanceUtc = request.NextMaintenanceUtc; equipment.NextCalibrationUtc = request.NextCalibrationUtc;
        equipment.PublicOperationalNotes = EmptyToNull(request.PublicOperationalNotes);
        equipment.UpdatedAtUtc = DateTimeOffset.UtcNow; equipment.UpdatedByUserId = actor.UserId;
        equipment.Location = await dbContext.Locations.SingleOrDefaultAsync(item => item.Id == request.LocationId, cancellationToken)
            ?? throw new ValidationException("La ubicación seleccionada no existe o no está disponible.");
        equipment.LocationId = equipment.Location.Id;
        equipment.SearchTextNormalized = BuildSearchText(equipment, equipment.Location);
        if (oldLocationId != equipment.LocationId)
            dbContext.EquipmentLocationHistory.Add(new() { EquipmentId = id, PreviousLocationId = oldLocationId,
                NewLocationId = equipment.LocationId, ChangedAtUtc = DateTimeOffset.UtcNow,
                Reason = request.ChangeReason.Trim(), ChangedByUserId = actor.UserId!.Value });
        if (!string.Equals(oldResponsibleName, equipment.ResponsibleName, StringComparison.Ordinal)
            || !string.Equals(oldResponsiblePosition, equipment.ResponsiblePosition, StringComparison.Ordinal))
            dbContext.EquipmentResponsibleHistory.Add(new()
            {
                EquipmentId = id,
                PreviousResponsible = oldResponsibleName,
                NewResponsible = equipment.ResponsibleName ?? "Sin responsable asignado",
                Position = equipment.ResponsiblePosition,
                AssignedAtUtc = DateTimeOffset.UtcNow,
                ChangedByUserId = actor.UserId!.Value
            });
        await notificationService.ResolveMissingLocationAsync(id, cancellationToken);
        if (oldStatus != equipment.Status) dbContext.EquipmentStatusHistory.Add(new() { EquipmentId = id, PreviousStatus = oldStatus, NewStatus = equipment.Status, ChangedAtUtc = DateTimeOffset.UtcNow, Reason = request.ChangeReason.Trim(), ChangedByUserId = actor.UserId!.Value });
        await auditService.AppendAsync(new(actor, "EQUIPMENT_UPDATED", "Equipment", id.ToString("D"), id,
            $"Se editó la ficha de {equipment.Name}. Motivo: {request.ChangeReason.Trim()}", oldValue,
            JsonSerializer.Serialize(new { equipment.InternalCode, equipment.Name, equipment.Model, equipment.SerialNumber, equipment.Status, equipment.Criticality, equipment.LocationId, equipment.ResponsibleName, equipment.ResponsiblePosition, equipment.NextMaintenanceUtc, equipment.NextCalibrationUtc })), cancellationToken);
        return await GetDetailsWithoutAuditAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, DeleteEquipmentRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        if (actor.Role != UserRole.MasterAdministrator) throw new ForbiddenException("Solo el Administrador Maestro puede dar de baja un equipo.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ValidationException("Debe indicar el motivo de la baja.");
        var equipment = await dbContext.Equipment.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new NotFoundException("No se encontró el equipo.");
        equipment.IsDeleted = true; equipment.DeletedAtUtc = DateTimeOffset.UtcNow; equipment.DeletedByUserId = actor.UserId; equipment.DeletionReason = request.Reason.Trim();
        await auditService.AppendAsync(new(actor, "EQUIPMENT_DELETED", "Equipment", id.ToString("D"), id, $"Se dio de baja lógica el equipo {equipment.Name}. Motivo: {equipment.DeletionReason}"), cancellationToken);
    }

    public async Task<CalibrationSummaryDto> AddCalibrationAsync(Guid equipmentId, AddCalibrationRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureEditor(actor);
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Laboratory)) throw new ValidationException("El laboratorio o profesional responsable es obligatorio.");
        if (request.NextCalibrationUtc is not null && request.NextCalibrationUtc <= request.PerformedAtUtc) throw new ValidationException("La próxima calibración debe ser posterior.");
        var equipment = await dbContext.Equipment.SingleOrDefaultAsync(item => item.Id == equipmentId, cancellationToken) ?? throw new NotFoundException("No se encontró el equipo.");
        var record = new Calibration { EquipmentId = equipmentId, PerformedAtUtc = request.PerformedAtUtc, NextCalibrationUtc = request.NextCalibrationUtc,
            Laboratory = request.Laboratory.Trim(), CertificateNumber = EmptyToNull(request.CertificateNumber), Result = request.Result,
            Traceability = EmptyToNull(request.Traceability), Notes = EmptyToNull(request.Notes), CreatedByUserId = actor.UserId };
        dbContext.Calibrations.Add(record); equipment.RequiresCalibration = true; equipment.NextCalibrationUtc = request.NextCalibrationUtc; equipment.UpdatedAtUtc = DateTimeOffset.UtcNow; equipment.UpdatedByUserId = actor.UserId;
        await auditService.AppendAsync(new(actor, "CALIBRATION_ADDED", "Calibration", record.Id.ToString("D"), equipmentId, $"Se agregó calibración a {equipment.Name}.", null, JsonSerializer.Serialize(request)), cancellationToken);
        return MapCalibration(record);
    }

    public async Task<FailureSummaryDto> AddFailureAsync(Guid equipmentId, AddFailureRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureEditor(actor);
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.ReportedBy)) throw new ValidationException("Descripción y persona que reporta son obligatorias.");
        var equipment = await dbContext.Equipment.SingleOrDefaultAsync(item => item.Id == equipmentId, cancellationToken) ?? throw new NotFoundException("No se encontró el equipo.");
        var record = new FailureRecord { EquipmentId = equipmentId, ReportedAtUtc = request.ReportedAtUtc, Description = request.Description.Trim(), ReportedBy = request.ReportedBy.Trim(), Service = EmptyToNull(request.Service), Impact = EmptyToNull(request.Impact), EquipmentStopped = request.EquipmentStopped, ImmediateAction = EmptyToNull(request.ImmediateAction), Diagnosis = EmptyToNull(request.Diagnosis), Solution = EmptyToNull(request.Solution), DowntimeMinutes = request.DowntimeMinutes, ClosedAtUtc = request.ClosedAtUtc, CreatedByUserId = actor.UserId };
        dbContext.Failures.Add(record);
        await auditService.AppendAsync(new(actor, "FAILURE_ADDED", "FailureRecord", record.Id.ToString("D"), equipmentId, $"Se registró una falla en {equipment.Name}.", null, JsonSerializer.Serialize(request)), cancellationToken);
        return MapFailure(record);
    }

    public async Task<IncidentSummaryDto> AddIncidentAsync(Guid equipmentId, AddIncidentRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureEditor(actor);
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.EventType) || string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.ReportedBy)) throw new ValidationException("Tipo de evento, descripción y persona que reporta son obligatorios.");
        var equipment = await dbContext.Equipment.SingleOrDefaultAsync(item => item.Id == equipmentId, cancellationToken) ?? throw new NotFoundException("No se encontró el equipo.");
        var record = new TechnoVigilanceIncident { EquipmentId = equipmentId, OccurredAtUtc = request.OccurredAtUtc, EventType = request.EventType.Trim(), Description = request.Description.Trim(), Consequence = EmptyToNull(request.Consequence), ReportedBy = request.ReportedBy.Trim(), Status = request.Status, ActionsTaken = EmptyToNull(request.ActionsTaken), WasReportedExternally = request.WasReportedExternally, ExternalReportNumber = EmptyToNull(request.ExternalReportNumber), ExternalReportDateUtc = request.ExternalReportDateUtc, ReportedEntity = EmptyToNull(request.ReportedEntity), Conclusions = EmptyToNull(request.Conclusions), CreatedByUserId = actor.UserId };
        dbContext.TechnoVigilanceIncidents.Add(record);
        await auditService.AppendAsync(new(actor, "INCIDENT_ADDED", "TechnoVigilanceIncident", record.Id.ToString("D"), equipmentId, $"Se registró un incidente de tecnovigilancia en {equipment.Name}.", null, JsonSerializer.Serialize(request)), cancellationToken);
        return MapIncident(record);
    }

    public async Task ChangeStatusAsync(Guid equipmentId, ChangeEquipmentStatusRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        EnsureEditor(actor);
        await locationAccess.EnsureEquipmentAccessAsync(equipmentId, actor, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ValidationException("El motivo del cambio de estado es obligatorio.");
        var equipment = await dbContext.Equipment.SingleOrDefaultAsync(item => item.Id == equipmentId, cancellationToken)
            ?? throw new NotFoundException("No se encontró el equipo.");
        if (equipment.Status == request.NewStatus) throw new ValidationException("El equipo ya tiene ese estado.");

        var oldStatus = equipment.Status;
        equipment.Status = request.NewStatus;
        equipment.UpdatedAtUtc = DateTimeOffset.UtcNow;
        equipment.UpdatedByUserId = actor.UserId;
        dbContext.EquipmentStatusHistory.Add(new()
        {
            EquipmentId = equipment.Id,
            PreviousStatus = oldStatus,
            NewStatus = request.NewStatus,
            ChangedAtUtc = DateTimeOffset.UtcNow,
            Reason = request.Reason.Trim(),
            ChangedByUserId = actor.UserId!.Value
        });
        await auditService.AppendAsync(new(actor, "EQUIPMENT_STATUS_CHANGED", "Equipment", equipment.Id.ToString("D"), equipment.Id,
            $"Se cambió el estado de {equipment.Name}.", oldStatus.ToString(), request.NewStatus.ToString()), cancellationToken);
    }

    public async Task<IReadOnlyList<LocationDto>> GetLocationsAsync(RequestContext actor, CancellationToken cancellationToken = default)
    {
        var scope = await locationAccess.GetScopeAsync(actor, cancellationToken);
        IQueryable<Location> query = dbContext.Locations.AsNoTracking();
        if (!scope.HasAllLocations)
            query = query.Where(location => scope.LocationIds.Contains(location.Id));
        var locations = await query.OrderBy(location => location.Institution)
            .ThenBy(location => location.Service).ToListAsync(cancellationToken);
        return locations.Select(MapLocation).ToList();
    }

    public async Task<LocationDto> CreateLocationAsync(CreateLocationRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        if (actor.Role != UserRole.MasterAdministrator) throw new ForbiddenException("Solo el Administrador Maestro puede crear ubicaciones.");
        if (string.IsNullOrWhiteSpace(request.Institution) || string.IsNullOrWhiteSpace(request.Site) || string.IsNullOrWhiteSpace(request.Service))
            throw new ValidationException("Institución, sede y servicio son obligatorios.");
        var location = new Location
        {
            Institution = request.Institution.Trim(),
            Site = request.Site.Trim(),
            Building = EmptyToNull(request.Building),
            Floor = EmptyToNull(request.Floor),
            Service = request.Service.Trim(),
            Area = EmptyToNull(request.Area),
            Room = EmptyToNull(request.Room),
            SpecificLocation = EmptyToNull(request.SpecificLocation),
            CreatedByUserId = actor.UserId
        };
        location.SearchTextNormalized = TextNormalizer.JoinNormalized(location.Institution, location.Site, location.Building,
            location.Floor, location.Service, location.Area, location.Room, location.SpecificLocation);
        dbContext.Locations.Add(location);
        await auditService.AppendAsync(new(actor, "LOCATION_CREATED", "Location", location.Id.ToString("D"), null,
            $"Se creó la ubicación {location.DisplayName}."), cancellationToken);
        return MapLocation(location);
    }

    public async Task<LocationDto> UpdateLocationAsync(Guid id, UpdateLocationRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        if (actor.Role != UserRole.MasterAdministrator) throw new ForbiddenException("Solo el Administrador Maestro puede modificar ubicaciones.");
        if (string.IsNullOrWhiteSpace(request.ChangeReason)) throw new ValidationException("Debe indicar el motivo de la modificación.");
        ValidateLocation(request.Institution, request.Site, request.Service);
        var location = await dbContext.Locations.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("No se encontró la ubicación.");
        var previous = JsonSerializer.Serialize(new { location.Institution, location.Site, location.Building, location.Floor, location.Service, location.Area, location.Room, location.SpecificLocation });
        ApplyLocation(location, request.Institution, request.Site, request.Building, request.Floor, request.Service,
            request.Area, request.Room, request.SpecificLocation);
        location.UpdatedAtUtc = DateTimeOffset.UtcNow;
        location.UpdatedByUserId = actor.UserId;
        var affected = await dbContext.Equipment.Where(item => item.LocationId == id).ToListAsync(cancellationToken);
        foreach (var equipment in affected) equipment.SearchTextNormalized = BuildSearchText(equipment, location);
        await auditService.AppendAsync(new(actor, "LOCATION_UPDATED", "Location", id.ToString("D"), null,
            $"Se modificó la ubicación. Motivo: {request.ChangeReason.Trim()}", previous,
            JsonSerializer.Serialize(new { location.Institution, location.Site, location.Building, location.Floor, location.Service, location.Area, location.Room, location.SpecificLocation })), cancellationToken);
        return MapLocation(location);
    }

    public async Task DeleteLocationAsync(Guid id, DeleteLocationRequest request, RequestContext actor, CancellationToken cancellationToken = default)
    {
        if (actor.Role != UserRole.MasterAdministrator) throw new ForbiddenException("Solo el Administrador Maestro puede eliminar ubicaciones.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ValidationException("Debe indicar el motivo de eliminación.");
        var location = await dbContext.Locations.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("No se encontró la ubicación.");
        var previousName = location.DisplayName;
        var affected = await dbContext.Equipment.Where(item => item.LocationId == id).ToListAsync(cancellationToken);
        foreach (var equipment in affected)
        {
            equipment.LocationId = null;
            equipment.Location = null;
            equipment.SearchTextNormalized = BuildSearchText(equipment, null);
            equipment.UpdatedAtUtc = DateTimeOffset.UtcNow;
            equipment.UpdatedByUserId = actor.UserId;
            dbContext.EquipmentLocationHistory.Add(new() { EquipmentId = equipment.Id, PreviousLocationId = id,
                NewLocationId = null, ChangedAtUtc = DateTimeOffset.UtcNow,
                Reason = $"Ubicación eliminada: {request.Reason.Trim()}", ChangedByUserId = actor.UserId!.Value });
            await notificationService.CreateMissingLocationAsync(equipment.Id, equipment.Name, id, previousName, actor, cancellationToken);
        }
        location.IsDeleted = true;
        location.DeletedAtUtc = DateTimeOffset.UtcNow;
        location.DeletedByUserId = actor.UserId;
        location.DeletionReason = request.Reason.Trim();
        await auditService.AppendAsync(new(actor, "LOCATION_DELETED", "Location", id.ToString("D"), null,
            $"Se eliminó lógicamente la ubicación {previousName}. {affected.Count} equipo(s) quedaron sin ubicación. Motivo: {request.Reason.Trim()}"), cancellationToken);
    }

    private async Task<Equipment> GetEntityAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Equipment.AsNoTracking().Include(equipment => equipment.Location)
            .Include(equipment => equipment.Maintenances).Include(equipment => equipment.Calibrations)
            .Include(equipment => equipment.StatusHistory).Include(equipment => equipment.Failures).Include(equipment => equipment.Incidents)
            .AsSplitQuery()
            .SingleOrDefaultAsync(equipment => equipment.Id == id, cancellationToken)
        ?? throw new NotFoundException("No se encontró el equipo solicitado.");

    private async Task<EquipmentDetailsDto> GetDetailsWithoutAuditAsync(Guid id, CancellationToken cancellationToken) =>
        MapDetails(await GetEntityAsync(id, cancellationToken));

    private static EquipmentSearchItemDto MapSearch(Equipment equipment) => new(
        equipment.Id, equipment.PublicId, equipment.InternalCode, equipment.Name, equipment.Brand,
        equipment.Model, equipment.SerialNumber, LocationName(equipment.Location), equipment.Status,
        equipment.Maintenances.OrderByDescending(x => x.PerformedAtUtc).Select(x => (DateTimeOffset?)x.PerformedAtUtc).FirstOrDefault(),
        equipment.NextMaintenanceUtc, equipment.Criticality);

    private static EquipmentDetailsDto MapDetails(Equipment equipment) => new(
        equipment.Id, equipment.PublicId, equipment.InternalCode, equipment.Name, equipment.EquipmentType,
        equipment.Category, equipment.Description, equipment.Manufacturer, equipment.Brand, equipment.Model,
        equipment.SerialNumber, equipment.InventoryNumber, equipment.AssetCode, equipment.LocationId, LocationName(equipment.Location),
        equipment.ResponsibleName, equipment.ResponsiblePosition, equipment.Status, equipment.Criticality,
        equipment.RequiresCalibration, equipment.NextMaintenanceUtc, equipment.NextCalibrationUtc,
        equipment.PublicOperationalNotes,
        equipment.Maintenances.OrderByDescending(item => item.PerformedAtUtc).Select(MapMaintenance).ToList(),
        equipment.Calibrations.OrderByDescending(item => item.PerformedAtUtc).Select(MapCalibration).ToList(),
        equipment.Failures.OrderByDescending(item => item.ReportedAtUtc).Select(MapFailure).ToList(),
        equipment.Incidents.OrderByDescending(item => item.OccurredAtUtc).Select(MapIncident).ToList(),
        equipment.StatusHistory.OrderByDescending(item => item.ChangedAtUtc)
            .Select(item => new StatusHistoryDto(item.Id, item.PreviousStatus, item.NewStatus, item.ChangedAtUtc, item.Reason)).ToList());

    private static MaintenanceSummaryDto MapMaintenance(MaintenanceRecord record) => new(
        record.Id, record.Type, record.PerformedAtUtc, record.PerformedBy, record.Activities, record.Findings,
        record.TechnicalDiagnosis, record.ActionsTaken, record.SparePartsSummary, record.Notes, record.Result, record.NextMaintenanceUtc);

    private static CalibrationSummaryDto MapCalibration(Calibration item) => new(item.Id, item.PerformedAtUtc, item.Laboratory,
        item.CertificateNumber, item.Result, item.Traceability, item.Notes, item.NextCalibrationUtc);
    private static FailureSummaryDto MapFailure(FailureRecord item) => new(item.Id, item.ReportedAtUtc, item.Description,
        item.ReportedBy, item.Impact, item.EquipmentStopped, item.ImmediateAction, item.Diagnosis, item.Solution, item.ClosedAtUtc);
    private static IncidentSummaryDto MapIncident(TechnoVigilanceIncident item) => new(item.Id, item.OccurredAtUtc,
        item.EventType, item.Description, item.Consequence, item.ReportedBy, item.Status, item.ActionsTaken,
        item.WasReportedExternally, item.ExternalReportNumber, item.ReportedEntity, item.Conclusions);

    private static LocationDto MapLocation(Location location) => new(location.Id, location.Institution, location.Site,
        location.Building, location.Floor, location.Service, location.Area, location.Room, location.SpecificLocation, location.DisplayName);

    private static string LocationName(Location? location) => location?.DisplayName ?? "Ubicación no disponible";

    private static double Score(Equipment equipment, string query) => new[]
    {
        DamerauLevenshtein.Similarity(equipment.Name, query),
        DamerauLevenshtein.Similarity(equipment.Model ?? string.Empty, query),
        DamerauLevenshtein.Similarity(equipment.Brand ?? string.Empty, query),
        DamerauLevenshtein.Similarity(equipment.Manufacturer ?? string.Empty, query),
        DamerauLevenshtein.Similarity(equipment.Location?.SearchTextNormalized ?? string.Empty, query)
    }.Max();

    private static string BuildSearchText(Equipment equipment, Location? location) => TextNormalizer.JoinNormalized(
        equipment.InternalCode, equipment.Name, equipment.EquipmentType, equipment.Category, equipment.Manufacturer,
        equipment.Brand, equipment.Model, equipment.SerialNumber, equipment.InventoryNumber, equipment.AssetCode,
        equipment.ResponsibleName, location?.SearchTextNormalized);

    private static void ValidateCreate(CreateEquipmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InternalCode)) throw new ValidationException("El código interno es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ValidationException("El nombre del equipo es obligatorio.");
        if (request.LocationId is null) throw new ValidationException("La ubicación del equipo es obligatoria.");
        if (request.NextCalibrationUtc is not null && !request.RequiresCalibration)
            throw new ValidationException("No puede asignar próxima calibración a un equipo que no requiere calibración.");
    }

    private static void EnsureEditor(RequestContext actor)
    {
        if (actor.UserId is null || actor.Role is not (UserRole.Editor or UserRole.MasterAdministrator))
            throw new ForbiddenException("Esta acción requiere permisos de Editor o Administrador Maestro.");
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateLocation(string institution, string site, string service)
    {
        if (string.IsNullOrWhiteSpace(institution) || string.IsNullOrWhiteSpace(site) || string.IsNullOrWhiteSpace(service))
            throw new ValidationException("Institución, sede y servicio son obligatorios.");
    }

    private static void ApplyLocation(Location location, string institution, string site, string? building, string? floor,
        string service, string? area, string? room, string? specificLocation)
    {
        location.Institution = institution.Trim();
        location.Site = site.Trim();
        location.Building = EmptyToNull(building);
        location.Floor = EmptyToNull(floor);
        location.Service = service.Trim();
        location.Area = EmptyToNull(area);
        location.Room = EmptyToNull(room);
        location.SpecificLocation = EmptyToNull(specificLocation);
        location.SearchTextNormalized = TextNormalizer.JoinNormalized(location.Institution, location.Site, location.Building,
            location.Floor, location.Service, location.Area, location.Room, location.SpecificLocation);
    }
}
