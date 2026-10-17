using BioVic.Application.Services;
using BioVic.Domain.Enums;

namespace BioVic.Tests;

public sealed class EquipmentRuleEngineTests
{
    [Fact]
    public void Evaluate_FlagsOverdueMaintenanceCalibrationAndMissingData()
    {
        var now = new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
        var input = new EquipmentRuleInput(EquipmentStatus.OutOfService, now.AddDays(-3), true, now.AddDays(-1),
            false, false, true, false, false, false, false);

        var alerts = EquipmentRuleEngine.Evaluate(input, now);

        Assert.Contains(alerts, alert => alert.Code == "MAINTENANCE_OVERDUE" && alert.Severity == AlertSeverity.High);
        Assert.Contains(alerts, alert => alert.Code == "CALIBRATION_OVERDUE");
        Assert.Contains(alerts, alert => alert.Code == "OUT_OF_SERVICE" && alert.Severity == AlertSeverity.Critical);
        Assert.Contains(alerts, alert => alert.Code == "MISSING_PHOTO");
    }

    [Fact]
    public void Evaluate_DoesNotReportPresentTechnicalReferencesAsMissing()
    {
        var now = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
        var input = new EquipmentRuleInput(EquipmentStatus.Operational, now.AddDays(90), true, now.AddDays(90),
            true, true, true, true, true, true, true);

        var alerts = EquipmentRuleEngine.Evaluate(input, now);

        Assert.DoesNotContain(alerts, alert => alert.Code == "MISSING_PHOTO");
        Assert.DoesNotContain(alerts, alert => alert.Code == "MISSING_MANUAL");
        Assert.DoesNotContain(alerts, alert => alert.Code == "MISSING_TECHNICAL_SHEET");
    }}
