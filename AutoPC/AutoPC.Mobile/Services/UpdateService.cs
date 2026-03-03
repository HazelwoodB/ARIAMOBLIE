namespace AutoPC.Client.Services;

using System.Text.Json;

public class UpdateService
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public UpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ConnectionHealth> CheckHealthAsync(string serverUrl, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeBaseUrl(serverUrl);

        var serverOk = false;
        var modelOk = false;
        string serverMessage;
        string modelMessage;

        try
        {
            using var serverResponse = await _httpClient.GetAsync($"{normalized}/api/test", cancellationToken);
            serverOk = serverResponse.IsSuccessStatusCode;
            serverMessage = serverOk
                ? "Connected"
                : $"HTTP {(int)serverResponse.StatusCode}";
        }
        catch (Exception ex)
        {
            serverMessage = $"Offline: {ex.Message}";
        }

        try
        {
            using var modelResponse = await _httpClient.GetAsync($"{normalized}/api/ollama/health", cancellationToken);
            modelOk = modelResponse.IsSuccessStatusCode;
            modelMessage = modelOk
                ? "Ollama ready"
                : $"Ollama unavailable (HTTP {(int)modelResponse.StatusCode})";
        }
        catch (Exception ex)
        {
            modelMessage = $"Ollama unreachable: {ex.Message}";
        }

        return new ConnectionHealth
        {
            NormalizedServerUrl = normalized,
            ServerOnline = serverOk,
            OllamaOnline = modelOk,
            ServerStatus = serverMessage,
            OllamaStatus = modelMessage,
            CheckedAt = DateTime.UtcNow
        };
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(
        string serverUrl,
        int currentBuild,
        string currentVersion,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeBaseUrl(serverUrl);

        try
        {
            using var response = await _httpClient.GetAsync($"{normalized}/api/mobile/update", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult
                {
                    HasUpdate = false,
                    Message = $"Update check failed (HTTP {(int)response.StatusCode})."
                };
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, JsonOptions);
            if (manifest == null)
            {
                return new UpdateCheckResult
                {
                    HasUpdate = false,
                    Message = "Invalid update manifest."
                };
            }

            var hasUpdate = manifest.LatestBuild > currentBuild;
            if (!hasUpdate)
            {
                return new UpdateCheckResult
                {
                    HasUpdate = false,
                    Manifest = manifest,
                    Message = $"You are up to date ({currentVersion} / build {currentBuild})."
                };
            }

            return new UpdateCheckResult
            {
                HasUpdate = true,
                Manifest = manifest,
                Message = $"Update available: {manifest.LatestVersion} (build {manifest.LatestBuild})."
            };
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult
            {
                HasUpdate = false,
                Message = $"Update check error: {ex.Message}"
            };
        }
    }

    private static string NormalizeBaseUrl(string url)
    {
        var normalized = url.Trim().TrimEnd('/');
        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"https://{normalized}";
        }

        return normalized;
    }
}

public class ConnectionHealth
{
    public string NormalizedServerUrl { get; set; } = string.Empty;
    public bool ServerOnline { get; set; }
    public bool OllamaOnline { get; set; }
    public string ServerStatus { get; set; } = string.Empty;
    public string OllamaStatus { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
}

public class UpdateManifest
{
    public string LatestVersion { get; set; } = string.Empty;
    public int LatestBuild { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public bool Mandatory { get; set; }
    public DateTime PublishedAtUtc { get; set; }
}

public class UpdateCheckResult
{
    public bool HasUpdate { get; set; }
    public string Message { get; set; } = string.Empty;
    public UpdateManifest? Manifest { get; set; }
}
