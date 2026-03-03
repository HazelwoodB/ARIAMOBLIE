using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AutoPC.Client.Services;
using AutoPC.Client.Services.Foundation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure HttpClient with the server base address
// The BaseAddress will be automatically set to the host environment's base address
// which in a hosted Blazor WASM app is the server URL
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
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
builder.Services.AddScoped<ConversationalNaturalnessEngine>(); // Turing test enhancement

// Register client-side services
builder.Services.AddScoped<ClientLLMService>();
builder.Services.AddScoped<ClientSentimentService>();
builder.Services.AddScoped<ClientChatManager>();

await builder.Build().RunAsync();
