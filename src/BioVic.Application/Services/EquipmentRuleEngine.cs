using BioVic.Domain.Enums;

namespace BioVic.Application.Services;

public sealed record EquipmentRuleInput(
    EquipmentStatus Status,
    DateTimeOffset? NextMaintenanceUtc,
    bool RequiresCalibration,
    DateTimeOffset? NextCalibrationUtc,
    bool HasPhoto,
    bool HasManufacturer,
    bool HasModel,
    bool HasResponsible,
    bool HasLocation,
    bool HasManual,
    bool HasTechnicalSheet);

public sealed record EquipmentAlert(string Code, string Message, AlertSeverity Severity);

public static class EquipmentRuleEngine
{
    public static IReadOnlyList<EquipmentAlert> Evaluate(EquipmentRuleInput input, DateTimeOffset nowUtc, int upcomingDays = 30)
    {
        var alerts = new List<EquipmentAlert>();
        if (input.NextMaintenanceUtc is not null && input.NextMaintenanceUtc < nowUtc)
            alerts.Add(new("MAINTENANCE_OVERDUE", "El mantenimiento está vencido.", AlertSeverity.High));
        else if (input.NextMaintenanceUtc is not null && input.NextMaintenanceUtc <= nowUtc.AddDays(upcomingDays))
            alerts.Add(new("MAINTENANCE_UPCOMING", $"El mantenimiento vence en {Math.Max(0, (input.NextMaintenanceUtc.Value - nowUtc).Days)} días.", AlertSeverity.Medium));

        if (input.RequiresCalibration && input.NextCalibrationUtc is not null && input.NextCalibrationUtc < nowUtc)
            alerts.Add(new("CALIBRATION_OVERDUE", "La calibración está vencida.", AlertSeverity.High));
        if (input.Status == EquipmentStatus.OutOfService)
            alerts.Add(new("OUT_OF_SERVICE", "El equipo está fuera de servicio.", AlertSeverity.Critical));

        AddMissing(alerts, input.HasPhoto, "MISSING_PHOTO", "No tiene fotografía.");
        AddMissing(alerts, input.HasManufacturer, "MISSING_MANUFACTURER", "No registra fabricante.");
        AddMissing(alerts, input.HasModel, "MISSING_MODEL", "No registra modelo.");
        AddMissing(alerts, input.HasResponsible, "MISSING_RESPONSIBLE", "No tiene responsable asignado.");
        AddMissing(alerts, input.HasLocation, "MISSING_LOCATION", "La ubicación está incompleta.");
        AddMissing(alerts, input.HasManual, "MISSING_MANUAL", "No tiene manual aprobado.");
        AddMissing(alerts, input.HasTechnicalSheet, "MISSING_TECHNICAL_SHEET", "No tiene ficha técnica.");
        return alerts;
    }

    private static void AddMissing(ICollection<EquipmentAlert> alerts, bool condition, string code, string message)
    {
        if (!condition) alerts.Add(new(code, message, AlertSeverity.Low));
    }
}
