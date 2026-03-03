namespace AutoPC.Client.Services.Foundation;

/// <summary>
/// Manages user preferences and customization settings
/// Controls how ARIA behaves and responds to individual users
/// </summary>
public class PreferenceManager
{
    private readonly StorageService _storageService;
    private readonly UserProfileService _userProfileService;
    private UserPreferences? _currentPreferences;
    private const string PREFERENCES_STORAGE_KEY = "user_preferences";

    public PreferenceManager(
        StorageService storageService,
        UserProfileService userProfileService)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
    }

    /// <summary>
    /// Initialize or load user preferences
    /// Call after UserProfileService.InitializeAsync
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("[Preferences] Initializing user preferences");

            var profile = _userProfileService.GetCurrentProfile();
            if (profile == null)
            {
                Console.WriteLine("[Preferences] No user profile found, cannot initialize preferences");
                return;
            }

            // Try to load existing preferences
            _currentPreferences = await _storageService.LoadAsync<UserPreferences>(
                PREFERENCES_STORAGE_KEY,
                cancellationToken
            );

            // If no preferences exist, create defaults
            if (_currentPreferences == null)
            {
                _currentPreferences = new UserPreferences
                {
                    Id = Guid.NewGuid(),
                    UserId = profile.Id,
                    CommunicationStyle = "casual",
                    ResponseLength = 2,
                    EnableEmojis = true,
                    EnableHumor = true,
                    EnableGreetings = true,
                    EnableWellnessChecks = true,
                    SaveConversationHistory = true,
                    MaxHistoryDays = 30,
                    EnableContextAwareness = true,
                    EnableProactiveAlerts = true,
                    AlertSensitivity = 5
                };

                await SavePreferencesAsync(_currentPreferences, cancellationToken);
                Console.WriteLine("[Preferences] Created default preferences");
            }
            else
            {
                Console.WriteLine("[Preferences] Loaded existing preferences");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preferences] Error initializing: {ex.Message}");
            // Create default preferences on error
            _currentPreferences = CreateDefaultPreferences();
        }
    }

    /// <summary>
    /// Get current preferences
    /// </summary>
    public UserPreferences GetCurrentPreferences()
    {
        return _currentPreferences ?? CreateDefaultPreferences();
    }

    /// <summary>
    /// Update all preferences at once (for Settings page)
    /// </summary>
    public async Task<bool> UpdatePreferencesAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (preferences == null)
            {
                Console.WriteLine("[Preferences] Cannot update null preferences");
                return false;
            }

            _currentPreferences = preferences;
            _currentPreferences.LastUpdated = DateTime.UtcNow;
            await SavePreferencesAsync(_currentPreferences, cancellationToken);
            Console.WriteLine("[Preferences] All preferences updated");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preferences] Error updating preferences: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Update communication style
    /// Options: "casual", "formal", "technical"
    /// </summary>
    public async Task<bool> SetCommunicationStyleAsync(
        string style,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentPreferences == null)
                return false;

            if (!new[] { "casual", "formal", "technical" }.Contains(style))
            {
                Console.WriteLine($"[Preferences] Invalid communication style: {style}");
                return false;
            }

            _currentPreferences.CommunicationStyle = style;
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preferences] Error setting communication style: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Set response length preference
    /// 1 = brief, 2 = normal, 3 = detailed
    /// </summary>
    public async Task<bool> SetResponseLengthAsync(
        int length,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentPreferences == null)
                return false;

            if (length < 1 || length > 3)
            {
                Console.WriteLine($"[Preferences] Response length must be 1-3, got {length}");
                return false;
            }

            _currentPreferences.ResponseLength = length;
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preferences] Error setting response length: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Toggle emoji usage
    /// </summary>
    public async Task<bool> SetEmojiEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentPreferences == null)
                return false;

            _currentPreferences.EnableEmojis = enabled;
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preferences] Error setting emoji preference: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Toggle humor in responses
    /// </summary>
    public async Task<bool> SetHumorEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentPreferences == null)
                return false;

            _currentPreferences.EnableHumor = enabled;
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preferences] Error setting humor preference: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Set topics of interest
    /// </summary>
    public async Task<bool> SetTopicsAsync(
        string[] topics,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentPreferences == null)
                return false;

            _currentPreferences.Topics = topics ?? Array.Empty<string>();
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preferences] Error setting topics: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Toggle conversation history saving
    /// </summary>
    public async Task<bool> SetSaveHistoryAsync(
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentPreferences == null)
                return false;

            _currentPreferences.SaveConversationHistory = enabled;
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preferences] Error setting history preference: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Set alert sensitivity (1-10 scale)
    /// </summary>
    public async Task<bool> SetAlertSensitivityAsync(
        int sensitivity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentPreferences == null)
                return false;

            if (sensitivity < 1 || sensitivity > 10)
            {
                Console.WriteLine($"[Preferences] Sensitivity must be 1-10, got {sensitivity}");
                return false;
            }

            _currentPreferences.AlertSensitivity = sensitivity;
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preferences] Error setting alert sensitivity: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Reset all preferences to defaults
    /// </summary>
    public async Task<bool> ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = _userProfileService.GetCurrentProfile();
            if (profile == null)
                return false;

            _currentPreferences = CreateDefaultPreferences(profile.Id);
            return await SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preferences] Error resetting to defaults: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Export preferences as JSON
    /// </summary>
    public string ExportAsJson()
    {
        if (_currentPreferences == null)
            return "{}";

        return System.Text.Json.JsonSerializer.Serialize(
            _currentPreferences,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
        );
    }

    /// <summary>
    /// Save preferences to storage
    /// </summary>
    private async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentPreferences == null)
                return false;

            _currentPreferences.LastUpdated = DateTime.UtcNow;
            await SavePreferencesAsync(_currentPreferences, cancellationToken);
            Console.WriteLine("[Preferences] Saved");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preferences] Error saving: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Save preferences to storage
    /// </summary>
    private async Task SavePreferencesAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        await _storageService.SaveAsync(PREFERENCES_STORAGE_KEY, preferences, cancellationToken);
    }

    /// <summary>
    /// Create default preferences
    /// </summary>
    private UserPreferences CreateDefaultPreferences(Guid? userId = null)
    {
        return new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? Guid.Empty,
            CommunicationStyle = "casual",
            ResponseLength = 2,
            EnableEmojis = true,
            EnableHumor = true,
            Topics = Array.Empty<string>(),
            EnableGreetings = true,
            EnableWellnessChecks = true,
            SaveConversationHistory = true,
            MaxHistoryDays = 30,
            EnableContextAwareness = true,
            EnableProactiveAlerts = true,
            AlertSensitivity = 5
        };
    }
}
