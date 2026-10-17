using BioVic.Domain.Enums;

namespace BioVic.Desktop.Converters;

public sealed record EnumOption<T>(T Value, string Text) where T : struct, Enum;

public static class SpanishLabels
{
    public static IReadOnlyList<EnumOption<T>> Options<T>() where T : struct, Enum =>
        Enum.GetValues<T>().Select(x => new EnumOption<T>(x, Text(x))).ToList();

    public static string Text<T>(T value) where T : struct, Enum => value switch
    {
        EquipmentStatus.Operational => "Operativo", EquipmentStatus.OperationalWithObservations => "Operativo con observaciones",
        EquipmentStatus.InMaintenance => "En mantenimiento", EquipmentStatus.MaintenancePending => "Mantenimiento pendiente",
        EquipmentStatus.OutOfService => "Fuera de servicio", EquipmentStatus.InRepair => "En reparación",
        EquipmentStatus.AwaitingSparePart => "En espera de repuesto", EquipmentStatus.InCalibration => "En calibración",
        EquipmentStatus.Quarantine => "En cuarentena", EquipmentStatus.Retired => "Retirado", EquipmentStatus.Decommissioned => "Dado de baja",
        Criticality.Low => "Baja", Criticality.Medium => "Media", Criticality.High => "Alta", Criticality.Critical => "Crítica",
        MaintenanceType.Preventive => "Preventivo", MaintenanceType.Corrective => "Correctivo", MaintenanceType.Predictive => "Predictivo",
        MaintenanceType.Inspection => "Inspección", MaintenanceType.Verification => "Verificación", MaintenanceType.Other => "Otro",
        MaintenanceResult.Successful => "Exitoso", MaintenanceResult.SuccessfulWithObservations => "Exitoso con observaciones",
        MaintenanceResult.Unsuccessful => "No exitoso", MaintenanceResult.Pending => "Pendiente",
        CalibrationResult.Compliant => "Conforme", CalibrationResult.NonCompliant => "No conforme",
        CalibrationResult.Conditional => "Conforme condicionado", CalibrationResult.Pending => "Pendiente",
        IncidentStatus.Open => "Abierto", IncidentStatus.Investigating => "En investigación", IncidentStatus.Reported => "Reportado", IncidentStatus.Closed => "Cerrado",
        AlertSeverity.Information => "Información", AlertSeverity.Low => "Baja", AlertSeverity.Medium => "Media", AlertSeverity.High => "Alta", AlertSeverity.Critical => "Crítica",
        UserRole.Searcher => "Consulta", UserRole.Editor => "Editor", UserRole.MasterAdministrator => "Administrador Maestro",
        _ => value.ToString()
    };

    public static string Action(string value) => value switch
    {
        "APPLICATION_STARTED" => "Aplicación iniciada", "MASTER_CREATED" => "Administrador Maestro creado",
        "LOGIN_SUCCEEDED" => "Inicio de sesión", "LOGIN_FAILED" => "Ingreso rechazado", "LOGOUT" => "Cierre de sesión",
        "EQUIPMENT_SEARCH" => "Búsqueda de equipo", "EQUIPMENT_VIEWED" => "Consulta de hoja de vida",
        "EQUIPMENT_CREATED" => "Equipo creado", "EQUIPMENT_UPDATED" => "Equipo editado", "EQUIPMENT_DELETED" => "Equipo dado de baja",
        "EQUIPMENT_STATUS_CHANGED" => "Estado del equipo modificado",
        "MAINTENANCE_ADDED" => "Mantenimiento registrado", "CALIBRATION_ADDED" => "Calibración registrada", "FAILURE_ADDED" => "Falla registrada",
        "INCIDENT_ADDED" => "Incidente registrado", "QR_GENERATED" => "QR generado", "LIFE_SHEET_PDF_GENERATED" => "PDF generado",
        "LIFE_SHEET_EMAIL_SENT" => "Correo enviado", "AI_QUESTION" => "Consulta histórica a BioVic IA", "EQUIPMENT_RULE_REVIEW" => "Revisión automática local", "USER_CREATED" => "Usuario creado",
        "USER_ACCESS_UPDATED" => "Permisos actualizados", "USER_DELETED" => "Usuario eliminado", "QR_VIEWED" => "Consulta mediante QR",
        "QR_PDF_VIEWED" => "PDF consultado mediante QR",
        "LOCATION_CREATED" => "Ubicación creada", "LOCATION_UPDATED" => "Ubicación modificada", "LOCATION_DELETED" => "Ubicación eliminada",
        "NOTIFICATION_SNOOZED" => "Mensaje pospuesto", "NOTIFICATION_RESOLVED" => "Mensaje atendido", "LIFE_SHEET_EMAIL_FAILED" => "Error de correo",
        "DEMO_DATA_CREATED" => "Datos de demostración creados",
        _ => value.Replace('_', ' ').ToLowerInvariant()
    };
}
