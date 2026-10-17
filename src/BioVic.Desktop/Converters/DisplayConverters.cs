using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using BioVic.Domain.Enums;

namespace BioVic.Desktop.Converters;

public sealed class EquipmentStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is EquipmentStatus status ? status switch
    {
        EquipmentStatus.Operational => "Operativo",
        EquipmentStatus.OperationalWithObservations => "Operativo con observaciones",
        EquipmentStatus.InMaintenance => "En mantenimiento",
        EquipmentStatus.MaintenancePending => "Pendiente de mantenimiento",
        EquipmentStatus.OutOfService => "Fuera de servicio",
        EquipmentStatus.InRepair => "En reparación",
        EquipmentStatus.AwaitingSparePart => "Pendiente de repuesto",
        EquipmentStatus.InCalibration => "En calibración",
        EquipmentStatus.Quarantine => "En cuarentena",
        EquipmentStatus.Retired => "Retirado",
        EquipmentStatus.Decommissioned => "Dado de baja",
        _ => status.ToString()
    } : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class EquipmentStatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is EquipmentStatus status ? status switch
    {
        EquipmentStatus.Operational => new SolidColorBrush(Color.FromRgb(18, 126, 93)),
        EquipmentStatus.OperationalWithObservations => new SolidColorBrush(Color.FromRgb(167, 112, 0)),
        EquipmentStatus.OutOfService or EquipmentStatus.Decommissioned => new SolidColorBrush(Color.FromRgb(190, 58, 58)),
        EquipmentStatus.InMaintenance or EquipmentStatus.InRepair or EquipmentStatus.InCalibration => new SolidColorBrush(Color.FromRgb(31, 102, 170)),
        _ => new SolidColorBrush(Color.FromRgb(86, 108, 116))
    } : Brushes.Gray;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class AuditActionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => SpanishLabels.Action(value?.ToString() ?? string.Empty);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class EnumSpanishTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        EquipmentStatus x => SpanishLabels.Text(x), Criticality x => SpanishLabels.Text(x), MaintenanceType x => SpanishLabels.Text(x),
        MaintenanceResult x => SpanishLabels.Text(x), CalibrationResult x => SpanishLabels.Text(x), IncidentStatus x => SpanishLabels.Text(x),
        AlertSeverity x => SpanishLabels.Text(x), UserRole x => SpanishLabels.Text(x),
        _ => value?.ToString() ?? string.Empty
    };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
