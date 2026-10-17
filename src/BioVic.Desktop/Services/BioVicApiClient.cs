using System.Net;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioVic.Application.DTOs;
using System.Net.Http;

namespace BioVic.Desktop.Services;

public sealed class BioVicApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public BioVicApiClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(120) };
        _httpClient.DefaultRequestHeaders.Add("X-BioVic-Computer", Environment.MachineName);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BioVic-Desktop/1.0.0");
    }

    public string BaseUrl => _httpClient.BaseAddress?.ToString() ?? string.Empty;
    public string? SessionToken { get; private set; }

    public async Task<SetupStatusDto> GetSetupStatusAsync(CancellationToken cancellationToken = default) =>
        await SendAsync<SetupStatusDto>(new(HttpMethod.Get, "api/setup/status"), cancellationToken);

    public async Task<AuthSessionDto> CreateMasterAsync(CreateMasterRequest request, CancellationToken cancellationToken = default)
    {
        var message = JsonRequest(HttpMethod.Post, "api/setup/master", request);
        var setupToken = Environment.GetEnvironmentVariable("BIOVIC_SETUP_TOKEN");
        if (!string.IsNullOrWhiteSpace(setupToken))
            message.Headers.TryAddWithoutValidation("X-BioVic-Setup-Token", setupToken);
        var result = await SendAsync<AuthSessionDto>(message, cancellationToken);
        SetSession(result.Token);
        return result;
    }

    public async Task<AuthSessionDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<AuthSessionDto>(JsonRequest(HttpMethod.Post, "api/auth/login", request), cancellationToken);
        SetSession(result.Token);
        return result;
    }

    public Task<MasterProfileDto> GetMasterProfileAsync(CancellationToken cancellationToken = default) =>
        SendAsync<MasterProfileDto>(new(HttpMethod.Get, "api/auth/master-profile"), cancellationToken);

    public Task<MasterProfileDto> UpdateMasterProfileAsync(UpdateMasterProfileRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<MasterProfileDto>(JsonRequest(HttpMethod.Put, "api/auth/master-profile", request), cancellationToken);

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (SessionToken is not null)
            await SendAsync(new(HttpMethod.Post, "api/auth/logout"), cancellationToken);
        SetSession(null);
    }

    public Task<PagedResult<EquipmentSearchItemDto>> SearchEquipmentAsync(string? query, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var path = $"api/equipment/search?q={Uri.EscapeDataString(query ?? string.Empty)}&page={page}&pageSize={pageSize}";
        return SendAsync<PagedResult<EquipmentSearchItemDto>>(new(HttpMethod.Get, path), cancellationToken);
    }

    public Task<EquipmentDetailsDto> GetEquipmentAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync<EquipmentDetailsDto>(new(HttpMethod.Get, $"api/equipment/{id:D}"), cancellationToken);

    public Task<EquipmentDetailsDto> CreateEquipmentAsync(CreateEquipmentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<EquipmentDetailsDto>(JsonRequest(HttpMethod.Post, "api/equipment", request), cancellationToken);

    public Task<EquipmentDetailsDto> UpdateEquipmentAsync(Guid id, UpdateEquipmentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<EquipmentDetailsDto>(JsonRequest(HttpMethod.Put, $"api/equipment/{id:D}", request), cancellationToken);

    public Task DeleteEquipmentAsync(Guid id, string reason, CancellationToken cancellationToken = default) =>
        SendAsync(JsonRequest(HttpMethod.Delete, $"api/equipment/{id:D}", new DeleteEquipmentRequest(reason)), cancellationToken);

    public Task<MaintenanceSummaryDto> AddMaintenanceAsync(Guid equipmentId, AddMaintenanceRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<MaintenanceSummaryDto>(JsonRequest(HttpMethod.Post, $"api/equipment/{equipmentId:D}/maintenance", request), cancellationToken);

    public Task<CalibrationSummaryDto> AddCalibrationAsync(Guid equipmentId, AddCalibrationRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<CalibrationSummaryDto>(JsonRequest(HttpMethod.Post, $"api/equipment/{equipmentId:D}/calibrations", request), cancellationToken);
    public Task<FailureSummaryDto> AddFailureAsync(Guid equipmentId, AddFailureRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<FailureSummaryDto>(JsonRequest(HttpMethod.Post, $"api/equipment/{equipmentId:D}/failures", request), cancellationToken);
    public Task<IncidentSummaryDto> AddIncidentAsync(Guid equipmentId, AddIncidentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<IncidentSummaryDto>(JsonRequest(HttpMethod.Post, $"api/equipment/{equipmentId:D}/incidents", request), cancellationToken);
    public Task<QrAccessInfoDto> GetQrAccessInfoAsync(Guid equipmentId, CancellationToken cancellationToken = default) =>
        SendAsync<QrAccessInfoDto>(new(HttpMethod.Get, $"api/equipment/{equipmentId:D}/qr/access"), cancellationToken);
    public Task<byte[]> GetQrAsync(Guid equipmentId, CancellationToken cancellationToken = default) => GetBytesAsync($"api/equipment/{equipmentId:D}/qr", cancellationToken);
    public Task<byte[]> GetLifeSheetPdfAsync(Guid equipmentId, CancellationToken cancellationToken = default) => GetBytesAsync($"api/equipment/{equipmentId:D}/life-sheet.pdf", cancellationToken);
    public Task<AiAnswerDto> AskAiAsync(Guid equipmentId, string question, CancellationToken cancellationToken = default) =>
        SendAsync<AiAnswerDto>(JsonRequest(HttpMethod.Post, $"api/equipment/{equipmentId:D}/ai", new AiQuestionRequest(question)), cancellationToken);

    public Task ChangeStatusAsync(Guid equipmentId, ChangeEquipmentStatusRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(JsonRequest(HttpMethod.Post, $"api/equipment/{equipmentId:D}/status", request), cancellationToken);

    public Task<IReadOnlyList<LocationDto>> GetLocationsAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<LocationDto>>(new(HttpMethod.Get, "api/locations"), cancellationToken);

    public Task<LocationDto> CreateLocationAsync(CreateLocationRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<LocationDto>(JsonRequest(HttpMethod.Post, "api/locations", request), cancellationToken);

    public Task<LocationDto> UpdateLocationAsync(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<LocationDto>(JsonRequest(HttpMethod.Put, $"api/locations/{id:D}", request), cancellationToken);

    public Task DeleteLocationAsync(Guid id, string reason, CancellationToken cancellationToken = default) =>
        SendAsync(JsonRequest(HttpMethod.Delete, $"api/locations/{id:D}", new DeleteLocationRequest(reason)), cancellationToken);

    public Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        SendAsync<DashboardDto>(new(HttpMethod.Get, "api/admin/dashboard"), cancellationToken);

    public Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<UserSummaryDto>>(new(HttpMethod.Get, "api/admin/users"), cancellationToken);

    public Task<UserSummaryDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<UserSummaryDto>(JsonRequest(HttpMethod.Post, "api/admin/users", request), cancellationToken);

    public Task<UserSummaryDto> UpdateUserAccessAsync(Guid id, UpdateUserAccessRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<UserSummaryDto>(JsonRequest(HttpMethod.Put, $"api/admin/users/{id:D}/access", request), cancellationToken);

    public Task DeleteUserAsync(Guid id, string reason, CancellationToken cancellationToken = default) =>
        SendAsync(JsonRequest(HttpMethod.Delete, $"api/admin/users/{id:D}", new DeleteUserRequest(reason)), cancellationToken);

    public Task<PagedResult<AuditLogDto>> GetAuditAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<AuditLogDto>>(new(HttpMethod.Get, $"api/admin/audit?page={page}&pageSize={pageSize}"), cancellationToken);

    public Task<AuditIntegrityDto> VerifyAuditAsync(CancellationToken cancellationToken = default) =>
        SendAsync<AuditIntegrityDto>(new(HttpMethod.Get, "api/admin/audit/integrity"), cancellationToken);

    public Task<IReadOnlyList<AdminNotificationDto>> GetAdminNotificationsAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<AdminNotificationDto>>(new(HttpMethod.Get, "api/admin/notifications"), cancellationToken);

    public Task SnoozeNotificationAsync(Guid id, DateTimeOffset untilUtc, CancellationToken cancellationToken = default) =>
        SendAsync(JsonRequest(HttpMethod.Post, $"api/admin/notifications/{id:D}/snooze", new SnoozeNotificationRequest(untilUtc)), cancellationToken);

    public Task ResolveNotificationAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(new(HttpMethod.Post, $"api/admin/notifications/{id:D}/resolve"), cancellationToken);

    public Task<IReadOnlyList<EquipmentDocumentDto>> GetEquipmentDocumentsAsync(Guid equipmentId,
        bool includeDeleted = false, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<EquipmentDocumentDto>>(new(HttpMethod.Get,
            $"api/equipment/{equipmentId:D}/documents?includeDeleted={includeDeleted.ToString().ToLowerInvariant()}"), cancellationToken);

    public async Task<EquipmentDocumentDto> UploadEquipmentDocumentAsync(Guid equipmentId, string filePath,
        string documentType, string? description, DateTimeOffset? documentDateUtc, DateTimeOffset? expiresAtUtc,
        bool approvedForAiKnowledge, Guid? supersedesDocumentId = null, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            81_920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeFor(filePath));
        content.Add(fileContent, "file", Path.GetFileName(filePath));
        content.Add(new StringContent(documentType), "documentType");
        if (!string.IsNullOrWhiteSpace(description)) content.Add(new StringContent(description.Trim()), "description");
        if (documentDateUtc is not null) content.Add(new StringContent(documentDateUtc.Value.ToString("O")), "documentDate");
        if (expiresAtUtc is not null) content.Add(new StringContent(expiresAtUtc.Value.ToString("O")), "expiresAt");
        content.Add(new StringContent(approvedForAiKnowledge.ToString().ToLowerInvariant()), "approvedForAiKnowledge");
        var path = supersedesDocumentId is Guid documentId
            ? $"api/equipment/{equipmentId:D}/documents/{documentId:D}/versions"
            : $"api/equipment/{equipmentId:D}/documents";
        return await SendAsync<EquipmentDocumentDto>(new(HttpMethod.Post, path) { Content = content }, cancellationToken);
    }

    public Task<byte[]> DownloadEquipmentDocumentAsync(Guid equipmentId, Guid documentId,
        CancellationToken cancellationToken = default) =>
        GetBytesAsync($"api/equipment/{equipmentId:D}/documents/{documentId:D}/download", cancellationToken);

    public Task ArchiveEquipmentDocumentAsync(Guid equipmentId, Guid documentId, string reason,
        CancellationToken cancellationToken = default) =>
        SendAsync(JsonRequest(HttpMethod.Delete, $"api/equipment/{equipmentId:D}/documents/{documentId:D}",
            new ArchiveDocumentRequest(reason)), cancellationToken);

    public Task<EquipmentDocumentDto> RestoreEquipmentDocumentAsync(Guid equipmentId, Guid documentId, string reason,
        CancellationToken cancellationToken = default) =>
        SendAsync<EquipmentDocumentDto>(JsonRequest(HttpMethod.Post,
            $"api/equipment/{equipmentId:D}/documents/{documentId:D}/restore", new RestoreDocumentRequest(reason)), cancellationToken);

    public Task<RecoverySummaryDto> GetRecoverySummaryAsync(CancellationToken cancellationToken = default) =>
        SendAsync<RecoverySummaryDto>(new(HttpMethod.Get, "api/admin/recovery"), cancellationToken);

    public Task RestoreDeletedItemAsync(DeletedItemDto item, string reason, CancellationToken cancellationToken = default)
    {
        var segment = item.ItemType switch
        {
            "Equipment" => "equipment",
            "User" => "users",
            "Location" => "locations",
            _ => throw new InvalidOperationException("Tipo de elemento eliminado no compatible.")
        };
        return SendAsync(JsonRequest(HttpMethod.Post, $"api/admin/recovery/{segment}/{item.Id:D}",
            new RestoreDeletedItemRequest(reason)), cancellationToken);
    }

    private HttpRequestMessage JsonRequest<T>(HttpMethod method, string path, T payload) => new(method, path)
    {
        Content = JsonContent.Create(payload, options: _jsonOptions)
    };

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        using (var response = await _httpClient.SendAsync(request, cancellationToken))
        {
            if (!response.IsSuccessStatusCode) throw await CreateExceptionAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken)
                ?? throw new BioVicApiException("BioVic recibió una respuesta vacía del servidor.");
        }
    }

    private async Task SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        using (var response = await _httpClient.SendAsync(request, cancellationToken))
        {
            if (!response.IsSuccessStatusCode) throw await CreateExceptionAsync(response, cancellationToken);
        }
    }

    private async Task<byte[]> GetBytesAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await CreateExceptionAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task<BioVicApiException> CreateExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var rawServerResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = $"El servidor rechazó la solicitud (HTTP {(int)response.StatusCode}).";

        if (!string.IsNullOrWhiteSpace(rawServerResponse))
        {
            try
            {
                using var document = JsonDocument.Parse(rawServerResponse);
                if (document.RootElement.TryGetProperty("detail", out var detail) &&
                    !string.IsNullOrWhiteSpace(detail.GetString()))
                    message = detail.GetString()!;
                else if (document.RootElement.TryGetProperty("title", out var title) &&
                         !string.IsNullOrWhiteSpace(title.GetString()))
                    message = title.GetString()!;
            }
            catch (JsonException)
            {
                // Las respuestas HTML o de texto no deben mostrarse completas en la interfaz.
            }
        }

        return new BioVicApiException(message, response.StatusCode);
    }
    private void SetSession(string? token)
    {
        SessionToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    public void Dispose() => _httpClient.Dispose();

    private static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
}

public sealed class BioVicApiException : Exception
{
    public BioVicApiException(string message, HttpStatusCode? statusCode = null) : base(message) =>
        StatusCode = statusCode;

    public HttpStatusCode? StatusCode { get; }
}

