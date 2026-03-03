namespace AutoPC.Client.Services;

using System.Collections.Concurrent;

/// <summary>
/// Manages chat operations entirely on the client side.
/// Uses server only for persistence and cross-client sync.
/// </summary>
public class ClientChatManager
{
    private readonly ClientLLMService _llmService;
    private readonly ClientSentimentService _sentimentService;
    private readonly HttpClient _httpClient;

    private List<ChatMessage> _conversationContext = [];
    private readonly ConcurrentDictionary<string, ChatMessage> _messageCache = new();
    private const int MaxContextMessages = 20;

    public ClientChatManager(
        ClientLLMService llmService,
        ClientSentimentService sentimentService,
        HttpClient httpClient)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _sentimentService = sentimentService ?? throw new ArgumentNullException(nameof(sentimentService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Process a user message and generate an AI response.
    /// Runs entirely on client; optionally syncs to server.
    /// </summary>
    public async Task<ClientChatResponse> ProcessMessageAsync(
        string userMessage,
        bool syncToServer = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        // Analyze user sentiment
        var sentimentResult = _sentimentService.AnalyzeSentiment(userMessage);

        // Create user message
        var userMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Role = "user",
            Message = userMessage,
            Timestamp = DateTime.UtcNow,
            Sentiment = sentimentResult.Label,
            Score = sentimentResult.Score
        };

        // Add to local context
        _conversationContext.Add(userMsg);
        _messageCache.TryAdd(userMsg.Id.ToString(), userMsg);
        MaintainContextWindow();

        // Optional: Sync to server
        if (syncToServer)
        {
            await SyncMessageToServerAsync(userMsg, cancellationToken);
        }

        // Generate AI response
        var aiResponse = await _llmService.GenerateReplyAsync(
            userMessage,
            _conversationContext,
            cancellationToken);

        // Create assistant message
        var assistantMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Role = "assistant",
            Message = aiResponse,
            Timestamp = DateTime.UtcNow,
            Sentiment = null,
            Score = null
        };

        // Add to local context
        _conversationContext.Add(assistantMsg);
        _messageCache.TryAdd(assistantMsg.Id.ToString(), assistantMsg);
        MaintainContextWindow();

        // Optional: Sync to server
        if (syncToServer)
        {
            await SyncMessageToServerAsync(assistantMsg, cancellationToken);
        }

        return new ClientChatResponse
        {
            UserMessage = userMsg,
            AssistantMessage = assistantMsg,
            Sentiment = sentimentResult.Label,
            Score = sentimentResult.Score
        };
    }

    /// <summary>
    /// Process message with streaming response and optional custom system prompt.
    /// </summary>
    public async IAsyncEnumerable<string> ProcessMessageStreamAsync(
        string userMessage,
        string? systemPrompt = null,
        bool syncToServer = false,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        // Analyze user sentiment
        var sentimentResult = _sentimentService.AnalyzeSentiment(userMessage);

        // Create and store user message
        var userMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Role = "user",
            Message = userMessage,
            Timestamp = DateTime.UtcNow,
            Sentiment = sentimentResult.Label,
            Score = sentimentResult.Score
        };

        _conversationContext.Add(userMsg);
        _messageCache.TryAdd(userMsg.Id.ToString(), userMsg);
        MaintainContextWindow();

        if (syncToServer)
        {
            await SyncMessageToServerAsync(userMsg, cancellationToken);
        }

        // Stream AI response with custom system prompt
        var fullResponse = new System.Text.StringBuilder();
        var assistantMsgId = Guid.NewGuid();

        await foreach (var chunk in _llmService.GenerateReplyStreamAsync(
            userMessage,
            _conversationContext,
            systemPrompt,
            cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            fullResponse.Append(chunk);
            yield return chunk;
        }

        // Store complete assistant message
        var assistantMsg = new ChatMessage
        {
            Id = assistantMsgId,
            Role = "assistant",
            Message = fullResponse.ToString(),
            Timestamp = DateTime.UtcNow,
            Sentiment = null,
            Score = null
        };

        _conversationContext.Add(assistantMsg);
        _messageCache.TryAdd(assistantMsg.Id.ToString(), assistantMsg);
        MaintainContextWindow();

        if (syncToServer)
        {
            await SyncMessageToServerAsync(assistantMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Load conversation history from server.
    /// </summary>
    public async Task LoadHistoryFromServerAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            // Skip server calls if no BaseAddress configured (standalone MAUI mode)
            if (_httpClient.BaseAddress == null)
            {
                Console.WriteLine("[ClientChat] No server configured - skipping history load");
                return;
            }
            var response = await _httpClient.GetAsync("/api/history", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var messages = System.Text.Json.JsonSerializer.Deserialize<List<ChatMessage>>(
                    json,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

                if (messages != null)
                {
                    _conversationContext = messages.TakeLast(limit).ToList();
                    foreach (var msg in _conversationContext)
                    {
                        _messageCache.TryAdd(msg.Id.ToString(), msg);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ClientChat] Error loading history: {ex.Message}");
        }
    }

    /// <summary>
    /// Clear local conversation context.
    /// </summary>
    public void ClearContext()
    {
        _conversationContext.Clear();
        _messageCache.Clear();
    }

    /// <summary>
    /// Get current conversation context.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetContext()
    {
        return _conversationContext.AsReadOnly();
    }

    /// <summary>
    /// Sync a message to the server for persistence.
    /// </summary>
    private async Task SyncMessageToServerAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Skip server calls if no BaseAddress configured (standalone MAUI mode)
            if (_httpClient.BaseAddress == null)
            {
                Console.WriteLine("[ClientChat] No server configured - skipping message sync");
                return;
            }
            var json = System.Text.Json.JsonSerializer.Serialize(message);
            var content = new StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/messages/sync", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[ClientChat] Failed to sync message: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ClientChat] Error syncing message: {ex.Message}");
        }
    }

    /// <summary>
    /// Maintain conversation context window size.
    /// </summary>
    private void MaintainContextWindow()
    {
        if (_conversationContext.Count > MaxContextMessages)
        {
            var toRemove = _conversationContext.Count - MaxContextMessages;
            _conversationContext.RemoveRange(0, toRemove);
        }
    }
}

/// <summary>
/// Response from processing a message.
/// </summary>
public class ClientChatResponse
{
    public ChatMessage UserMessage { get; set; } = null!;
    public ChatMessage AssistantMessage { get; set; } = null!;
    public string Sentiment { get; set; } = string.Empty;
    public float Score { get; set; }
}
