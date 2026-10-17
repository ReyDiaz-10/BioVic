using BioVic.Application.Services;

namespace BioVic.Tests;

public sealed class AuditHashCalculatorTests
{
    [Fact]
    public void Calculate_IsDeterministicAndSensitiveToChanges()
    {
        var timestamp = new DateTimeOffset(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        var original = new AuditHashInput(timestamp, "2026-08-19 05:00:00 -05:00",
            Guid.Parse("22222222-2222-2222-2222-222222222222"), "admin", "MasterAdministrator", "EQUIPMENT_CREATED",
            "Equipment", "eq-1", Guid.Parse("11111111-1111-1111-1111-111111111111"), "Equipo creado",
            null, "nuevo", "PC-01", "session-1", "127.0.0.1", "1.0.0", AuditHashCalculator.GenesisHash);
        var tampered = original with { Description = "Equipo alterado" };

        Assert.Equal(AuditHashCalculator.Calculate(original), AuditHashCalculator.Calculate(original));
        Assert.NotEqual(AuditHashCalculator.Calculate(original), AuditHashCalculator.Calculate(tampered));
    }
}
