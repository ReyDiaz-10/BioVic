using BioVic.Application.Contracts;
using BioVic.Application.DTOs;
using BioVic.Application.Services;
using BioVic.Domain.Entities;
using BioVic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BioVic.Infrastructure.Services;

public sealed class AuditService(BioVicDbContext dbContext) : IAuditService
{
    private static readonly SemaphoreSlim ChainLock = new(1, 1);

    public async Task<long> AppendAsync(AuditEntryRequest request, CancellationToken cancellationToken = default)
    {
        await ChainLock.WaitAsync(cancellationToken);
        try
        {
            var previousHash = await dbContext.AuditLogs
                .OrderByDescending(log => log.Id)
                .Select(log => log.RecordHash)
                .FirstOrDefaultAsync(cancellationToken) ?? AuditHashCalculator.GenesisHash;
            var now = DateTimeOffset.UtcNow;
            var log = new AuditLog
            {
                TimestampUtc = now,
                TimestampLocal = FormatBogotaTime(now),
                UserId = request.Actor.UserId,
                Username = request.Actor.Username,
                Role = request.Actor.Role,
                Action = request.Action,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                EquipmentId = request.EquipmentId,
                Description = request.Description,
                OldValue = request.OldValue,
                NewValue = request.NewValue,
                ComputerName = Limit(request.Actor.ComputerName, 255),
                SessionId = Limit(request.Actor.SessionId, 100),
                IpAddress = Limit(request.Actor.IpAddress, 64),
                ApplicationVersion = Limit(request.Actor.ApplicationVersion, 40),
                PreviousHash = previousHash
            };
            log.RecordHash = Calculate(log);
            dbContext.AuditLogs.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);
            return log.Id;
        }
        finally
        {
            ChainLock.Release();
        }
    }

    public async Task<PagedResult<AuditLogDto>> QueryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = dbContext.AuditLogs.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var entries = await query.OrderByDescending(log => log.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new AuditLogDto(
                log.Id, log.TimestampUtc, log.TimestampLocal, log.UserId, log.Username, log.Role,
                log.Action, log.EntityType, log.EntityId, log.EquipmentId, log.Description,
                log.OldValue, log.NewValue, log.ComputerName, log.IpAddress, log.ApplicationVersion,
                log.PreviousHash, log.RecordHash))
            .ToListAsync(cancellationToken);
        return new(entries, page, pageSize, total);
    }

    public async Task<AuditIntegrityDto> VerifyIntegrityAsync(CancellationToken cancellationToken = default)
    {
        var logs = await dbContext.AuditLogs.AsNoTracking().OrderBy(log => log.Id).ToListAsync(cancellationToken);
        var expectedPreviousHash = AuditHashCalculator.GenesisHash;
        foreach (var log in logs)
        {
            if (!string.Equals(log.PreviousHash, expectedPreviousHash, StringComparison.Ordinal) ||
                !string.Equals(log.RecordHash, Calculate(log), StringComparison.Ordinal))
            {
                return new(false, logs.Count, log.Id, "Se detectaron inconsistencias en el historial.");
            }

            expectedPreviousHash = log.RecordHash;
        }

        return new(true, logs.Count, null, "Historial íntegro.");
    }

    private static string Calculate(AuditLog log) => AuditHashCalculator.Calculate(new(
        log.TimestampUtc, log.TimestampLocal, log.UserId, log.Username, log.Role?.ToString(), log.Action, log.EntityType,
        log.EntityId, log.EquipmentId, log.Description, log.OldValue, log.NewValue,
        log.ComputerName, log.SessionId, log.IpAddress, log.ApplicationVersion, log.PreviousHash));

    private static string FormatBogotaTime(DateTimeOffset utc)
    {
        foreach (var id in new[] { "America/Bogota", "SA Pacific Standard Time" })
        {
            try
            {
                return TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.FindSystemTimeZoneById(id)).ToString("yyyy-MM-dd HH:mm:ss zzz");
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the next platform-specific identifier.
            }
        }

        return utc.ToOffset(TimeSpan.FromHours(-5)).ToString("yyyy-MM-dd HH:mm:ss zzz");
    }

    private static string Limit(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? "UNKNOWN" : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];
}
