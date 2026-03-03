using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AutoPC.Client.Services;
using AutoPC.Client.Services.Foundation;

namespace AutoPC.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Register IConfiguration (required by ClientLLMService)
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        builder.Services.AddSingleton<IConfiguration>(config);

        // Configure HttpClient for remote server communication
        // BaseAddress is set dynamically when user configures connection in Settings
        builder.Services.AddScoped(sp => new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(300)
        });

        // Register foundation services (must be registered first - others depend on these)
        builder.Services.AddScoped<StorageService>();
        builder.Services.AddScoped<UserProfileService>();
        builder.Services.AddScoped<PreferenceManager>();
        builder.Services.AddScoped<FeedbackCollector>();
        builder.Services.AddScoped<FeedbackLearningService>();

        // Register Phase 3 services - ARIA 3.0 enhancements
        builder.Services.AddScoped<EmotionRecognitionService>();
        builder.Services.AddScoped<PersonalityEngine>();
        builder.Services.AddScoped<ConversationalMemoryService>();
        builder.Services.AddScoped<RetroThemeService>();
        builder.Services.AddScoped<ConversationalNaturalnessEngine>();

        // Register client-side services
        builder.Services.AddScoped<ClientLLMService>();
        builder.Services.AddScoped<ClientSentimentService>();
        builder.Services.AddScoped<ClientChatManager>();
        builder.Services.AddScoped<UpdateService>();

        return builder.Build();
    }
}
