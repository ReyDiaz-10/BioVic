using BioVic.Infrastructure.Data;
using BioVic.Infrastructure.Options;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BioVic.Infrastructure.Services;

public sealed class SqliteBackupHostedService(
    BioVicOptions options,
    ILogger<SqliteBackupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.BackupEnabled || !options.DatabaseProvider.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
            return;

        await CreateVerifiedBackupAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(options.BackupIntervalHours));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await CreateVerifiedBackupAsync(stoppingToken);
    }

    private async Task CreateVerifiedBackupAsync(CancellationToken cancellationToken)
    {
        var backupDirectory = NormalizeDirectory(options.BackupPath);
        Directory.CreateDirectory(backupDirectory);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var backupFile = Path.Combine(backupDirectory, $"biovic-{timestamp}-{Guid.NewGuid():N}.db");

        try
        {
            var sourceConnectionString = DatabaseInitializer.NormalizeSqliteConnection(options.DatabaseConnection);
            await using var source = new SqliteConnection(sourceConnectionString);
            await using var destination = new SqliteConnection($"Data Source={backupFile}");
            await source.OpenAsync(cancellationToken);
            await destination.OpenAsync(cancellationToken);
            source.BackupDatabase(destination);

            await using var integrityCommand = destination.CreateCommand();
            integrityCommand.CommandText = "PRAGMA integrity_check;";
            var integrityResult = Convert.ToString(await integrityCommand.ExecuteScalarAsync(cancellationToken));
            if (!string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"La copia SQLite no superó integrity_check: {integrityResult ?? "sin resultado"}.");

            DeleteExpiredBackups(backupDirectory);
            logger.LogInformation("Copia de seguridad SQLite verificada: {BackupFile}.", backupFile);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (File.Exists(backupFile)) File.Delete(backupFile);
            logger.LogError(exception, "No fue posible crear o verificar la copia de seguridad SQLite.");
        }
    }

    private void DeleteExpiredBackups(string backupDirectory)
    {
        var cutoff = DateTime.UtcNow.AddDays(-options.BackupRetentionDays);
        foreach (var file in Directory.EnumerateFiles(backupDirectory, "biovic-*.db", SearchOption.TopDirectoryOnly))
        {
            if (File.GetLastWriteTimeUtc(file) < cutoff) File.Delete(file);
        }
    }

    private static string NormalizeDirectory(string path) => Path.IsPathRooted(path)
        ? Path.GetFullPath(path)
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}
