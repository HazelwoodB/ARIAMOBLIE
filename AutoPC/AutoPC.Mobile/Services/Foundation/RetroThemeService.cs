namespace AutoPC.Client.Services.Foundation;

/// <summary>
/// Simplified theme service for mobile stability.
/// Keeps one consistent ARIA theme across the app.
/// </summary>
public class RetroThemeService
{
    private readonly StorageService _storage;
    private const string ThemeKey = "aria_theme_settings";

    private static readonly ThemeSettings AriaCoreTheme = new()
    {
        Name = "ARIA Core",
        Description = "Single optimized theme for chat clarity and performance.",

        PrimaryColor = "#00f0ff",
        SecondaryColor = "#7bc8ff",
        AccentColor = "#ff2d7b",

        BackgroundPrimary = "#0a0a0f",
        BackgroundSecondary = "#111126",
        CardBackground = "#161633",

        TextPrimary = "#e8f0fe",
        TextSecondary = "#b9d9ff",
        TextMuted = "#7b8ca8",

        BorderColor = "#1a1a3e",
        ShadowColor = "rgba(0, 240, 255, 0.12)",
        GlowColor = "rgba(0, 240, 255, 0.3)",

        FontPrimary = "'Segoe UI', sans-serif",
        FontDisplay = "'Segoe UI', sans-serif",
        FontMono = "'Consolas', monospace",

        ScanLineColor = "#00f0ff",
        ScanLineOpacity = "0.03",
        EnableCRTEffect = false
    };

    private ThemeSettings _currentTheme = AriaCoreTheme;

    public RetroThemeService(StorageService storage)
    {
        _storage = storage;
    }

    public event EventHandler<ThemeSettings>? ThemeChanged;

    public async Task<ThemeSettings> GetCurrentThemeAsync()
    {
        var saved = await _storage.LoadAsync<ThemeSettings>(ThemeKey);
        _currentTheme = saved ?? AriaCoreTheme;
        return _currentTheme;
    }

    public async Task SetThemeAsync(string themeName)
    {
        _currentTheme = AriaCoreTheme;
        await _storage.SaveAsync(ThemeKey, _currentTheme);
        ThemeChanged?.Invoke(this, _currentTheme);
    }

    public async Task SetCustomThemeAsync(ThemeSettings customTheme)
    {
        _currentTheme = AriaCoreTheme;
        await _storage.SaveAsync(ThemeKey, _currentTheme);
        ThemeChanged?.Invoke(this, _currentTheme);
    }

    public Dictionary<string, ThemeSettings> GetAvailableThemes()
    {
        return new Dictionary<string, ThemeSettings>
        {
            ["AriaCore"] = AriaCoreTheme
        };
    }

    public string GenerateThemeCSS(ThemeSettings? theme = null)
    {
        theme ??= _currentTheme;

        return $@":root {{
    --aria-primary: {theme.PrimaryColor};
    --aria-secondary: {theme.SecondaryColor};
    --aria-accent: {theme.AccentColor};

    --aria-bg-primary: {theme.BackgroundPrimary};
    --aria-bg-secondary: {theme.BackgroundSecondary};
    --aria-bg-card: {theme.CardBackground};

    --aria-text-primary: {theme.TextPrimary};
    --aria-text-secondary: {theme.TextSecondary};
    --aria-text-muted: {theme.TextMuted};

    --aria-border: {theme.BorderColor};
    --aria-shadow: {theme.ShadowColor};
    --aria-glow: {theme.GlowColor};

    --aria-font-primary: {theme.FontPrimary};
    --aria-font-display: {theme.FontDisplay};
    --aria-font-mono: {theme.FontMono};

    --aria-scan-line-color: {theme.ScanLineColor};
    --aria-scan-line-opacity: {theme.ScanLineOpacity};
    --aria-crt-effect: {theme.EnableCRTEffect};
}}";
    }
}

public class ThemeSettings
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string PrimaryColor { get; set; } = "#00f0ff";
    public string SecondaryColor { get; set; } = "#7bc8ff";
    public string AccentColor { get; set; } = "#ff2d7b";

    public string BackgroundPrimary { get; set; } = "#0a0a0f";
    public string BackgroundSecondary { get; set; } = "#111126";
    public string CardBackground { get; set; } = "#161633";

    public string TextPrimary { get; set; } = "#e8f0fe";
    public string TextSecondary { get; set; } = "#b9d9ff";
    public string TextMuted { get; set; } = "#7b8ca8";

    public string BorderColor { get; set; } = "#1a1a3e";
    public string ShadowColor { get; set; } = "rgba(0, 240, 255, 0.12)";
    public string GlowColor { get; set; } = "rgba(0, 240, 255, 0.3)";

    public string FontPrimary { get; set; } = "'Segoe UI', sans-serif";
    public string FontDisplay { get; set; } = "'Segoe UI', sans-serif";
    public string FontMono { get; set; } = "'Consolas', monospace";

    public string ScanLineColor { get; set; } = "#00f0ff";
    public string ScanLineOpacity { get; set; } = "0.03";
    public bool EnableCRTEffect { get; set; } = false;
}
