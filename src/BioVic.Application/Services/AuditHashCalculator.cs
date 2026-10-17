using System.Security.Cryptography;
using System.Text;

namespace BioVic.Application.Services;

public sealed record AuditHashInput(
    DateTimeOffset TimestampUtc,
    string TimestampLocal,
    Guid? UserId,
    string Username,
    string? Role,
    string Action,
    string EntityType,
    string? EntityId,
    Guid? EquipmentId,
    string Description,
    string? OldValue,
    string? NewValue,
    string ComputerName,
    string SessionId,
    string IpAddress,
    string ApplicationVersion,
    string PreviousHash);

public static class AuditHashCalculator
{
    public const string GenesisHash = "GENESIS";

    public static string Calculate(AuditHashInput input)
    {
        var canonical = string.Join('|',
            input.TimestampUtc.ToUniversalTime().ToString("O"),
            input.TimestampLocal,
            input.UserId?.ToString("D") ?? string.Empty,
            input.Username,
            input.Role ?? string.Empty,
            input.Action,
            input.EntityType,
            input.EntityId ?? string.Empty,
            input.EquipmentId?.ToString("D") ?? string.Empty,
            input.Description,
            input.OldValue ?? string.Empty,
            input.NewValue ?? string.Empty,
            input.ComputerName,
            input.SessionId,
            input.IpAddress,
            input.ApplicationVersion,
            input.PreviousHash);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
