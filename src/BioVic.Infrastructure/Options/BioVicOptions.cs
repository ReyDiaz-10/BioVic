namespace BioVic.Infrastructure.Options;

public sealed class BioVicOptions
{
    public string EnvironmentName { get; init; } = "Development";
    public string HostingProvider { get; init; } = "standalone";
    public string AllowedHosts { get; init; } = "localhost;127.0.0.1";
    public bool RequireHttps { get; init; }
    public bool ReverseProxyEnabled { get; init; }
    public string TrustedProxyIps { get; init; } = "127.0.0.1;::1";
    public bool SetupEnabled { get; init; } = true;
    public string SetupToken { get; init; } = string.Empty;
    public int MaxRequestBodyBytes { get; init; } = 1_048_576;
    public string DatabaseProvider { get; init; } = "sqlite";
    public string DatabaseConnection { get; init; } = "Data Source=data/biovic.db";
    public bool BackupEnabled { get; init; } = true;
    public string BackupPath { get; init; } = "backups";
    public string LogPath { get; init; } = "logs";
    public string DataProtectionPath { get; init; } = "data-protection";
    public int BackupIntervalHours { get; init; } = 24;
    public int BackupRetentionDays { get; init; } = 30;
    public bool SerialMustBeUnique { get; init; } = true;
    public int SessionHours { get; init; } = 8;
    public int FailedLoginLimit { get; init; } = 5;
    public int LockoutMinutes { get; init; } = 15;
    public string ApiUrls { get; init; } = "http://127.0.0.1:5180";
    public string QrBaseUrl { get; init; } = "http://127.0.0.1:5180/e";
    public string AiProvider { get; init; } = "ollama";
    public string AiBaseUrl { get; init; } = "http://localhost:11434";
    public string AiModel { get; init; } = "qwen3:8b";
    public string AiApiKey { get; init; } = string.Empty;
}
