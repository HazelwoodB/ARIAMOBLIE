namespace AutoPC.Client.Services.Foundation;

using System.Text.Json;
using Microsoft.JSInterop;

/// <summary>
/// Abstraction for local storage operations using browser localStorage via JSInterop.
/// Handles all data persistence for feedback, profiles, preferences, and learning data.
/// </summary>
public class StorageService
{
    private const string STORAGE_PREFIX = "ARIA_";
    private readonly IJSRuntime _jsRuntime;
    private bool _jsReady = false;

    // In-memory fallback cache when JSInterop is not yet available (prerender)
    private readonly Dictionary<string, string> _memoryCache = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public StorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Save data to browser localStorage with key prefix
    /// </summary>
    public async Task SaveAsync<T>(string key, T data, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var storageKey = $"{STORAGE_PREFIX}{key}";
            var json = JsonSerializer.Serialize(data, _jsonOptions);

            // Always update in-memory cache
            _memoryCache[storageKey] = json;

            // Persist to browser localStorage
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, storageKey, json);
                _jsReady = true;
                Console.WriteLine($"[Storage] Saved {key} ({json.Length} bytes) to localStorage");
            }
            catch (InvalidOperationException)
            {
                // JSInterop not available yet (prerender) - data is in memory cache
                Console.WriteLine($"[Storage] Saved {key} ({json.Length} bytes) to memory cache (JS not ready)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Storage] Error saving {key}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Load data from browser localStorage
    /// </summary>
    public async Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var storageKey = $"{STORAGE_PREFIX}{key}";

            string? json = null;

            // Try browser localStorage first
            try
            {
                json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, storageKey);
                _jsReady = true;
            }
            catch (InvalidOperationException)
            {
                // JSInterop not available yet (prerender) - check memory cache
            }

            // Fallback to memory cache
            if (json == null && _memoryCache.TryGetValue(storageKey, out var cached))
            {
                json = cached;
            }

            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine($"[Storage] No data found for {key}");
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            Console.WriteLine($"[Storage] Loaded {key} ({json.Length} bytes)");
            return result;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[Storage] JSON parse error loading {key}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Storage] Error loading {key}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Delete data from storage
    /// </summary>
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = $"{STORAGE_PREFIX}{key}";
            _memoryCache.Remove(storageKey);

            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, storageKey);
                Console.WriteLine($"[Storage] Deleted {key}");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine($"[Storage] Deleted {key} from memory cache (JS not ready)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Storage] Error deleting {key}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Clear all ARIA data from storage
    /// </summary>
    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Clear();

            try
            {
                // Remove all keys with ARIA_ prefix from localStorage
                var keysJson = await _jsRuntime.InvokeAsync<string>(
                    "eval",
                    cancellationToken,
                    $"JSON.stringify(Object.keys(localStorage).filter(k => k.startsWith('{STORAGE_PREFIX}')))");

                if (!string.IsNullOrEmpty(keysJson))
                {
                    var keys = JsonSerializer.Deserialize<List<string>>(keysJson);
                    if (keys != null)
                    {
                        foreach (var key in keys)
                        {
                            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, key);
                        }
                    }
                }

                Console.WriteLine("[Storage] Cleared all ARIA data from localStorage");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("[Storage] Cleared ARIA memory cache (JS not ready)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Storage] Error clearing storage: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Check if a key exists in storage
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await LoadAsync<object>(key, cancellationToken);
            return data != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get approximate storage size in bytes for ARIA data
    /// </summary>
    public async Task<long> GetStorageSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sizeStr = await _jsRuntime.InvokeAsync<string>(
                "eval",
                cancellationToken,
                $"Object.keys(localStorage).filter(k => k.startsWith('{STORAGE_PREFIX}')).reduce((sum, k) => sum + localStorage.getItem(k).length, 0).toString()");

            return long.TryParse(sizeStr, out var size) ? size : 0;
        }
        catch
        {
            // Estimate from memory cache
            return _memoryCache.Values.Sum(v => (long)v.Length);
        }
    }
}
