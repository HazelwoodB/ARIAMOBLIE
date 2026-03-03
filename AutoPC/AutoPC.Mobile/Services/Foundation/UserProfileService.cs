namespace AutoPC.Client.Services.Foundation;

/// <summary>
/// Manages user profile creation, retrieval, and updates
/// Foundation service for personalization system
/// </summary>
public class UserProfileService
{
    private readonly StorageService _storageService;
    private UserProfile? _currentProfile;
    private const string PROFILE_STORAGE_KEY = "user_profile";

    public UserProfileService(StorageService storageService)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    }

    /// <summary>
    /// Initialize or load the current user profile
    /// Call this on app startup
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("[UserProfile] Initializing user profile");

            // Try to load existing profile
            _currentProfile = await _storageService.LoadAsync<UserProfile>(
                PROFILE_STORAGE_KEY,
                cancellationToken
            );

            // If no profile exists, create a new one
            if (_currentProfile == null)
            {
                _currentProfile = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    Username = "User",
                    DisplayName = "ARIA User",
                    TimeZone = GetSystemTimeZone(),
                    Locale = GetSystemLocale(),
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                await SaveProfileAsync(_currentProfile, cancellationToken);
                Console.WriteLine($"[UserProfile] Created new profile: {_currentProfile.Id}");
            }
            else
            {
                Console.WriteLine($"[UserProfile] Loaded existing profile: {_currentProfile.Id}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserProfile] Error initializing: {ex.Message}");
            // Create a default profile if initialization fails
            _currentProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                Username = "User",
                TimeZone = GetSystemTimeZone()
            };
        }
    }

    /// <summary>
    /// Get the current user profile
    /// </summary>
    public UserProfile? GetCurrentProfile()
    {
        return _currentProfile;
    }

    /// <summary>
    /// Get a specific profile by ID
    /// </summary>
    public async Task<UserProfile?> GetProfileByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // For now, only current profile supported
            // Future: Add multi-user support
            if (userId == _currentProfile?.Id)
            {
                return _currentProfile;
            }

            Console.WriteLine($"[UserProfile] Profile not found: {userId}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserProfile] Error retrieving profile: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Update user profile information
    /// </summary>
    public async Task<bool> UpdateProfileAsync(
        UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            profile.LastUpdated = DateTime.UtcNow;
            await SaveProfileAsync(profile, cancellationToken);
            _currentProfile = profile;

            Console.WriteLine($"[UserProfile] Updated profile: {profile.Id}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserProfile] Error updating profile: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Update user's display name
    /// </summary>
    public async Task<bool> UpdateDisplayNameAsync(
        string displayName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentProfile == null)
                return false;

            _currentProfile.DisplayName = displayName;
            return await UpdateProfileAsync(_currentProfile, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserProfile] Error updating display name: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Update user's timezone
    /// </summary>
    public async Task<bool> UpdateTimeZoneAsync(
        string timeZone,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentProfile == null)
                return false;

            _currentProfile.TimeZone = timeZone;
            return await UpdateProfileAsync(_currentProfile, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserProfile] Error updating timezone: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Add or update custom metadata
    /// </summary>
    public async Task<bool> SetMetadataAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentProfile == null)
                return false;

            _currentProfile.Metadata[key] = value;
            return await UpdateProfileAsync(_currentProfile, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserProfile] Error setting metadata: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get custom metadata value
    /// </summary>
    public string? GetMetadata(string key)
    {
        if (_currentProfile?.Metadata.TryGetValue(key, out var value) == true)
        {
            return value;
        }
        return null;
    }

    /// <summary>
    /// Delete the current user profile
    /// Warning: This is permanent!
    /// </summary>
    public async Task<bool> DeleteProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentProfile == null)
                return false;

            var profileId = _currentProfile.Id;
            await _storageService.DeleteAsync(PROFILE_STORAGE_KEY, cancellationToken);
            _currentProfile = null;

            Console.WriteLine($"[UserProfile] Deleted profile: {profileId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserProfile] Error deleting profile: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Save profile to storage
    /// </summary>
    private async Task SaveProfileAsync(
        UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        await _storageService.SaveAsync(PROFILE_STORAGE_KEY, profile, cancellationToken);
    }

    /// <summary>
    /// Get system timezone
    /// </summary>
    private string GetSystemTimeZone()
    {
        // In Blazor WASM, you'd use JS interop to get this
        // For now, return UTC as default
        return TimeZoneInfo.Local.Id;
    }

    /// <summary>
    /// Get system locale
    /// </summary>
    private string GetSystemLocale()
    {
        // In Blazor WASM, you'd get this from browser
        return System.Globalization.CultureInfo.CurrentCulture.Name;
    }
}
