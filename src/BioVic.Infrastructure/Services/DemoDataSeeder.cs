using System.Text.Json;
using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Exceptions;
using BioVic.Application.Services;
using BioVic.Domain.Entities;
using BioVic.Domain.Enums;
using BioVic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BioVic.Infrastructure.Services;

public sealed class DemoDataSeeder(BioVicDbContext dbContext, IAuditService auditService)
{
    public async Task<int> SeedAsync(RequestContext actor, CancellationToken cancellationToken = default)
    {
        if (actor.Role != UserRole.MasterAdministrator)
            throw new ForbiddenException("Solo el Administrador Maestro puede generar datos de demostración.");
        if (await dbContext.Equipment.AnyAsync(cancellationToken))
            throw new ConflictException("La generación de demostración solo está disponible cuando el inventario está vacío.");

        var locations = new[]
        {
            NewLocation("Hospital BioVic Demo", "Sede Principal", "Urgencias", "Reanimación"),
            NewLocation("Hospital BioVic Demo", "Sede Principal", "UCI Adultos", "Cubículos"),
            NewLocation("Hospital BioVic Demo", "Sede Principal", "Hospitalización", "Piso 3"),
            NewLocation("Hospital BioVic Demo", "Sede Ambulatoria", "Consulta externa", "Procedimientos")
        };
        foreach (var location in locations) location.CreatedByUserId = actor.UserId;
        dbContext.Locations.AddRange(locations);

        var catalog = new[]
        {
            ("Monitor multiparamétrico", "Monitoreo", "DemoMed", "MX-500"),
            ("Desfibrilador", "Reanimación", "DemoLife", "DF-20"),
            ("Ventilador mecánico", "Soporte vital", "DemoAir", "V-10"),
            ("Bomba de infusión", "Infusión", "DemoFlow", "BF-8"),
            ("Electrocardiógrafo", "Diagnóstico", "DemoCardio", "ECG-12"),
            ("Aspirador de secreciones", "Soporte", "DemoVac", "AS-3"),
            ("Oxímetro de pulso", "Monitoreo", "DemoMed", "OX-2"),
            ("Autoclave", "Esterilización", "DemoClean", "AC-100")
        };

        for (var index = 0; index < 20; index++)
        {
            var item = catalog[index % catalog.Length];
            var location = locations[index % locations.Length];
            var equipment = new Equipment
            {
                InternalCode = $"DEMO-{index + 1:000}",
                Name = item.Item1,
                EquipmentType = item.Item2,
                Category = item.Item2,
                Manufacturer = item.Item3,
                Brand = item.Item3,
                Model = item.Item4,
                SerialNumber = $"DEMO-SN-{index + 1:0000}",
                Location = location,
                ResponsibleName = "Responsable ficticio",
                ResponsiblePosition = "Referente de servicio",
                Status = index % 9 == 0 ? EquipmentStatus.OutOfService : index % 6 == 0 ? EquipmentStatus.InMaintenance : EquipmentStatus.Operational,
                Criticality = index % 4 == 0 ? Criticality.High : Criticality.Medium,
                RequiresCalibration = index % 3 == 0,
                NextMaintenanceUtc = DateTimeOffset.UtcNow.AddDays(index - 5),
                NextCalibrationUtc = index % 3 == 0 ? DateTimeOffset.UtcNow.AddDays(index - 3) : null,
                PublicOperationalNotes = "Dato completamente ficticio para demostración.",
                CreatedByUserId = actor.UserId
            };
            equipment.SearchTextNormalized = TextNormalizer.JoinNormalized(equipment.InternalCode, equipment.Name,
                equipment.EquipmentType, equipment.Manufacturer, equipment.Brand, equipment.Model,
                equipment.SerialNumber, location.SearchTextNormalized);
            dbContext.Equipment.Add(equipment);
            dbContext.EquipmentStatusHistory.Add(new()
            {
                EquipmentId = equipment.Id,
                PreviousStatus = equipment.Status,
                NewStatus = equipment.Status,
                ChangedAtUtc = DateTimeOffset.UtcNow,
                Reason = "Estado inicial de dato ficticio.",
                ChangedByUserId = actor.UserId!.Value
            });
        }

        await auditService.AppendAsync(new(actor, "DEMO_DATA_CREATED", "Equipment", null, null,
            "Se generaron datos completamente ficticios para demostración.", null,
            JsonSerializer.Serialize(new { EquipmentCount = 20, LocationCount = locations.Length })), cancellationToken);
        return 20;
    }

    private static Location NewLocation(string institution, string site, string service, string area)
    {
        var location = new Location { Institution = institution, Site = site, Service = service, Area = area };
        location.SearchTextNormalized = TextNormalizer.JoinNormalized(institution, site, service, area);
        return location;
    }
}
