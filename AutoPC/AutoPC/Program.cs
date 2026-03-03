using AutoPC.Client.Pages;
using AutoPC.Components;
using AutoPC.Models;
using AutoPC.Services;
using AutoPC.Data;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        // Add database context
        builder.Services.AddDbContext<ChatDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("ChatDb")));

        // Register ML model service
        builder.Services.AddSingleton<SimpleSentimentModel>();
        builder.Services.AddHttpClient("llm");
        builder.Services.AddSingleton<LLMAssistantService>();
        builder.Services.AddScoped<ChatLogService>();
        builder.Services.AddScoped<AssistantChatModel>();
        builder.Services.AddScoped<FeedbackService>();

        // Enable CORS for PWA / mobile clients connecting from other origins
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AriaClients", policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader());
        });

        WebApplication app = builder.Build();

        // Initialize database on startup
        app.Services.InitializeDatabase();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        // Enable CORS for remote PWA / mobile clients
        app.UseCors("AriaClients");

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(AutoPC.Client._Imports).Assembly);

        // Minimal API endpoints for chatbot and model prediction

        // Debug endpoint
        app.MapGet("/api/test", () =>
        {
            return Results.Ok(new { message = "API is reachable (client-side processing mode)", timestamp = DateTime.UtcNow });
        });

        // Health endpoint for Ollama availability (used by mobile status checks)
        app.MapGet("/api/ollama/health", async () =>
        {
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                using var response = await httpClient.GetAsync("http://localhost:11434/api/tags");

                if (!response.IsSuccessStatusCode)
                {
                    return Results.StatusCode(503);
                }

                return Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Ollama health error: {ex.Message}");
                return Results.StatusCode(503);
            }
        });

        // Mobile update manifest endpoint
        app.MapGet("/api/mobile/update", (IConfiguration configuration) =>
        {
            var section = configuration.GetSection("MobileUpdate");

            var latestVersion = section["LatestVersion"] ?? "3.0";
            var latestBuild = int.TryParse(section["LatestBuild"], out var build) ? build : 1;
            var downloadUrl = section["DownloadUrl"] ?? string.Empty;
            var githubOwner = section["GitHubOwner"] ?? string.Empty;
            var githubRepo = section["GitHubRepo"] ?? string.Empty;
            var releaseTag = section["ReleaseTag"] ?? string.Empty;
            var assetName = section["AssetName"] ?? "ARIA-v3.0.apk";
            var releaseNotes = section["ReleaseNotes"] ?? "Stability and reliability improvements.";
            var mandatory = bool.TryParse(section["Mandatory"], out var required) && required;

            if (string.IsNullOrWhiteSpace(downloadUrl)
                && !string.IsNullOrWhiteSpace(githubOwner)
                && !string.IsNullOrWhiteSpace(githubRepo)
                && !string.IsNullOrWhiteSpace(releaseTag)
                && !string.IsNullOrWhiteSpace(assetName))
            {
                downloadUrl = $"https://github.com/{githubOwner}/{githubRepo}/releases/download/{releaseTag}/{assetName}";
            }

            return Results.Ok(new
            {
                latestVersion,
                latestBuild,
                downloadUrl,
                releaseNotes,
                mandatory,
                publishedAtUtc = DateTime.UtcNow
            });
        });

        // ============ FEEDBACK API ENDPOINTS ============

        // Submit feedback from client
        app.MapPost("/api/feedback/submit", async ([FromServices] FeedbackService feedbackService, [FromBody] FeedbackSubmitRequest req) =>
        {
            if (req is null || req.Rating < 1 || req.Rating > 5)
            {
                return Results.BadRequest(new { error = "Valid rating (1-5) is required." });
            }

            try
            {
                var entry = await feedbackService.SubmitFeedbackAsync(req);
                Console.WriteLine($"[API] Feedback submitted: {entry.Rating}★ for message {entry.MessageId}");
                return Results.Ok(new { id = entry.Id, rating = entry.Rating, isHelpful = entry.IsHelpful, saved = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/feedback/submit: {ex.Message}");
                return Results.StatusCode(500);
            }
        });

        // Get feedback statistics
        app.MapGet("/api/feedback/stats", async ([FromServices] FeedbackService feedbackService) =>
        {
            try
            {
                var stats = await feedbackService.GetStatsAsync();
                return Results.Ok(stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/feedback/stats: {ex.Message}");
                return Results.StatusCode(500);
            }
        });

        // Get feedback for a specific message
        app.MapGet("/api/feedback/message/{messageId:guid}", async ([FromServices] FeedbackService feedbackService, Guid messageId) =>
        {
            try
            {
                var entry = await feedbackService.GetFeedbackForMessageAsync(messageId);
                return entry is not null ? Results.Ok(entry) : Results.NotFound();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/feedback/message: {ex.Message}");
                return Results.StatusCode(500);
            }
        });

        // Get all feedback for a user
        app.MapGet("/api/feedback/user/{userId:guid}", async ([FromServices] FeedbackService feedbackService, Guid userId, [FromQuery] int limit = 100) =>
        {
            try
            {
                var entries = await feedbackService.GetFeedbackForUserAsync(userId, limit);
                return Results.Ok(entries);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/feedback/user: {ex.Message}");
                return Results.StatusCode(500);
            }
        });

        // Export all feedback (for analysis/backup)
        app.MapGet("/api/feedback/export", async ([FromServices] FeedbackService feedbackService, [FromQuery] int limit = 500) =>
        {
            try
            {
                var entries = await feedbackService.GetAllFeedbackAsync(limit);
                return Results.Ok(entries);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/feedback/export: {ex.Message}");
                return Results.StatusCode(500);
            }
        });

        // Force ML model retrain with all collected feedback
        app.MapPost("/api/feedback/retrain", async ([FromServices] FeedbackService feedbackService) =>
        {
            try
            {
                await feedbackService.ForceRetrainAsync();
                return Results.Ok(new { retrained = true, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/feedback/retrain: {ex.Message}");
                return Results.StatusCode(500);
            }
        });

        // Get ML model diagnostics
        app.MapGet("/api/feedback/model", ([FromServices] SimpleSentimentModel model) =>
        {
            return Results.Ok(model.GetDiagnostics());
        });

        // ============ END FEEDBACK API ENDPOINTS ============

        // Legacy endpoints kept for compatibility but note they're not used in client-side mode
        app.MapPost("/api/chat", async ([FromServices] AssistantChatModel model, [FromBody] ChatRequest req) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Message))
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            try
            {
                var response = await model.ProcessUserMessageAsync(req.Message).ConfigureAwait(false);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        });

        // NEW: Get all messages (for cross-client sync)
        app.MapGet("/api/history", ([FromServices] ChatLogService chatLogService) =>
        {
            try
            {
                var history = chatLogService.GetHistory(200);
                return Results.Ok(history);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/history: {ex}");
                return Results.StatusCode(500);
            }
        });

        // NEW: Sync single message to server
        app.MapPost("/api/messages/sync", ([FromServices] ChatLogService chatLogService, [FromBody] ChatMessage message) =>
        {
            if (message is null)
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            try
            {
                chatLogService.AddMessage(message);
                Console.WriteLine($"[API] Message synced: {message.Role} - {message.Message.Substring(0, Math.Min(50, message.Message.Length))}");
                return Results.Ok(new { id = message.Id, synced = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/messages/sync: {ex}");
                return Results.StatusCode(500);
            }
        });

        // NEW: Sync batch of messages
        app.MapPost("/api/messages/sync-batch", ([FromServices] ChatLogService chatLogService, [FromBody] List<ChatMessage> messages) =>
        {
            if (messages is null || messages.Count == 0)
            {
                return Results.BadRequest(new { error = "Messages are required." });
            }

            try
            {
                foreach (var msg in messages)
                {
                    chatLogService.AddMessage(msg);
                }
                Console.WriteLine($"[API] Batch sync: {messages.Count} messages");
                return Results.Ok(new { synced = messages.Count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/messages/sync-batch: {ex}");
                return Results.StatusCode(500);
            }
        });

        // NEW: Get messages for specific user or conversation
        app.MapGet("/api/messages", ([FromServices] ChatLogService chatLogService, [FromQuery] int limit = 100) =>
        {
            try
            {
                var messages = chatLogService.GetHistory(limit);
                return Results.Ok(messages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/messages: {ex}");
                return Results.StatusCode(500);
            }
        });

        // NEW: Get single message by ID
        app.MapGet("/api/messages/{id:guid}", ([FromServices] ChatLogService chatLogService, Guid id) =>
        {
            try
            {
                var message = chatLogService.GetHistory(500).FirstOrDefault(m => m.Id == id);
                if (message is null)
                    return Results.NotFound();
                return Results.Ok(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/messages/{{id}}: {ex}");
                return Results.StatusCode(500);
            }
        });

        // Legacy streaming endpoint (not used in new architecture)
        app.MapPost("/api/chat/stream", async (HttpContext http, [FromServices] AssistantChatModel model, [FromBody] ChatRequest req) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Message))
            {
                http.Response.StatusCode = 400;
                await http.Response.WriteAsync("Message is required.");
                return;
            }

            http.Response.ContentType = "text/plain; charset=utf-8";
            http.Response.Headers.CacheControl = "no-cache";

            try
            {
                await foreach (var chunk in model.ProcessUserMessageStreamAsync(req.Message, http.RequestAborted))
                {
                    if (http.RequestAborted.IsCancellationRequested)
                        break;

                    var bytes = System.Text.Encoding.UTF8.GetBytes(chunk);
                    await http.Response.Body.WriteAsync(bytes, http.RequestAborted).ConfigureAwait(false);
                    await http.Response.Body.FlushAsync(http.RequestAborted).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // client cancelled
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error in /api/chat/stream: {ex}");
            }
        });

        // Legacy sentiment endpoint
        app.MapPost("/api/predict", ([FromServices] AssistantChatModel model, [FromBody] ChatRequest req) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Message))
            {
                return Results.BadRequest(new { error = "Message is required." });
            }

            try
            {
                var sentiment = model.AnalyzeSentiment(req.Message);
                return Results.Ok(sentiment);
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        });

        app.MapGet("/api/stats", ([FromServices] AssistantChatModel model) =>
        {
            var stats = model.GetConversationStats();
            return Results.Ok(stats);
        });

        app.MapPost("/api/chat/init-context", ([FromServices] AssistantChatModel model) =>
        {
            model.LoadConversationHistory();
            return Results.Ok(new { message = "Context loaded successfully." });
        });

        app.MapPost("/api/chat/clear-context", ([FromServices] AssistantChatModel model) =>
        {
            model.ClearContext();
            return Results.Ok(new { message = "Context cleared." });
        });

        // Ollama proxy endpoint - forwards requests from browser client to local Ollama server
        // This avoids CORS issues and browser timeout limitations
        app.MapPost("/api/ollama/chat", async (HttpContext http) =>
        {
            try
            {
                using var reader = new StreamReader(http.Request.Body);
                var body = await reader.ReadToEndAsync();

                var ollamaUrl = "http://localhost:11434/api/chat";

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(300) };
                using var request = new HttpRequestMessage(HttpMethod.Post, ollamaUrl);
                request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

                // Check if streaming is requested
                var isStream = body.Contains("\"stream\":true") || body.Contains("\"stream\": true");

                if (isStream)
                {
                    http.Response.ContentType = "application/x-ndjsonc; charset=utf-8";
                    http.Response.Headers.CacheControl = "no-cache";

                    var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, http.RequestAborted);
                    if (!response.IsSuccessStatusCode)
                    {
                        http.Response.StatusCode = (int)response.StatusCode;
                        await http.Response.WriteAsync(await response.Content.ReadAsStringAsync());
                        return;
                    }

                    using var stream = await response.Content.ReadAsStreamAsync(http.RequestAborted);
                    await stream.CopyToAsync(http.Response.Body, http.RequestAborted);
                }
                else
                {
                    var response = await httpClient.SendAsync(request, http.RequestAborted);
                    http.Response.StatusCode = (int)response.StatusCode;
                    http.Response.ContentType = "application/json";
                    var responseBody = await response.Content.ReadAsStringAsync();
                    await http.Response.WriteAsync(responseBody);
                }
            }
            catch (OperationCanceledException)
            {
                // Client cancelled
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Ollama proxy error: {ex.Message}");
                http.Response.StatusCode = 502;
                await http.Response.WriteAsync($"{{\"error\": \"Ollama proxy error: {ex.Message}\"}}");
            }
        });

        app.Run();
    }
}