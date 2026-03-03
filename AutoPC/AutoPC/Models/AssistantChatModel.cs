namespace AutoPC.Models;

using AutoPC.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Comprehensive assistant chat model that orchestrates all LLM and ML capabilities.
/// Provides ChatGPT-level functionality including context management, sentiment analysis,
/// streaming responses, and conversation history.
/// </summary>
public class AssistantChatModel
{
    private readonly LLMAssistantService _llmService;
    private readonly SimpleSentimentModel _sentimentModel;
    private readonly ChatLogService _chatLogService;
    
    private List<ChatMessage> _conversationContext = [];
    private const int MaxContextMessages = 20; // Keep last N messages for context

    public AssistantChatModel(
        LLMAssistantService llmService,
        SimpleSentimentModel sentimentModel,
        ChatLogService chatLogService)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _sentimentModel = sentimentModel ?? throw new ArgumentNullException(nameof(sentimentModel));
        _chatLogService = chatLogService ?? throw new ArgumentNullException(nameof(chatLogService));
    }

    /// <summary>
    /// Initializes the conversation context from stored history.
    /// </summary>
    public async Task InitializeContextAsync(int contextLimit = MaxContextMessages)
    {
        var history = await _chatLogService.GetHistoryAsync(contextLimit);
        _conversationContext = history.ToList();
    }

    /// <summary>
    /// Initializes the conversation context from stored history (synchronous).
    /// </summary>
    public void InitializeContext(int contextLimit = MaxContextMessages)
    {
        var history = _chatLogService.GetHistory(contextLimit);
        _conversationContext = history.ToList();
    }

    /// <summary>
    /// Processes a user message and generates an assistant response.
    /// Includes sentiment analysis and context management.
    /// </summary>
    public async Task<ChatResponse> ProcessUserMessageAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        // Create and log user message
        var userMsg = CreateUserMessage(userMessage);
        await _chatLogService.AddMessageAsync(userMsg);
        _conversationContext.Add(userMsg);
        MaintainContextWindow();

        // Generate assistant response
        var reply = await _llmService.GenerateReplyAsync(userMessage, cancellationToken)
            .ConfigureAwait(false);

        // Create and log assistant message
        var assistantMsg = CreateAssistantMessage(reply);
        await _chatLogService.AddMessageAsync(assistantMsg);
        _conversationContext.Add(assistantMsg);
        MaintainContextWindow();

        return new ChatResponse
        {
            Reply = reply,
            Sentiment = userMsg.Sentiment ?? string.Empty,
            Score = userMsg.Score ?? 0
        };
    }

    /// <summary>
    /// Streams a response from the assistant, yielding chunks as they arrive.
    /// Useful for real-time UI updates.
    /// </summary>
    public async IAsyncEnumerable<string> ProcessUserMessageStreamAsync(
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty.", nameof(userMessage));

        // Create and log user message
        var userMsg = CreateUserMessage(userMessage);
        await _chatLogService.AddMessageAsync(userMsg);
        _conversationContext.Add(userMsg);
        MaintainContextWindow();

        // Stream assistant response
        var fullResponse = new System.Text.StringBuilder();
        await foreach (var chunk in _llmService.GenerateReplyStreamAsync(userMessage, cancellationToken)
            .ConfigureAwait(false))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            fullResponse.Append(chunk);
            yield return chunk;
        }

        // Log complete assistant message
        var assistantMsg = CreateAssistantMessage(fullResponse.ToString());
        await _chatLogService.AddMessageAsync(assistantMsg);
        _conversationContext.Add(assistantMsg);
        MaintainContextWindow();
    }

    /// <summary>
    /// Gets the current conversation history from context.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetConversationHistory()
    {
        return _conversationContext.AsReadOnly();
    }

    /// <summary>
    /// Loads historical conversation for resume/continuation.
    /// </summary>
    public async Task LoadConversationHistoryAsync(int limit = MaxContextMessages)
    {
        await InitializeContextAsync(limit);
    }

    /// <summary>
    /// Loads historical conversation for resume/continuation (synchronous).
    /// </summary>
    public void LoadConversationHistory(int limit = MaxContextMessages)
    {
        InitializeContext(limit);
    }

    /// <summary>
    /// Clears the in-memory conversation context.
    /// Note: Does not clear persistent chat logs.
    /// </summary>
    public void ClearContext()
    {
        _conversationContext.Clear();
    }

    /// <summary>
    /// Gets full conversation history from persistent storage.
    /// </summary>
    public IEnumerable<ChatMessage> GetFullHistory(int limit = 200)
    {
        return _chatLogService.GetHistory(limit);
    }

    /// <summary>
    /// Analyzes sentiment of a given text.
    /// </summary>
    public SentimentAnalysis AnalyzeSentiment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty.", nameof(text));

        var sentiment = _sentimentModel.Predict(text);
        return new SentimentAnalysis
        {
            Label = sentiment.Label,
            Score = sentiment.Score,
            IsPositive = sentiment.Label == "Positive"
        };
    }

    /// <summary>
    /// Gets conversation statistics.
    /// </summary>
    public ConversationStats GetConversationStats()
    {
        var history = _conversationContext;
        var userMessages = history.Where(m => m.Role == "user").ToList();
        var assistantMessages = history.Where(m => m.Role == "assistant").ToList();

        var avgUserSentiment = userMessages
            .Where(m => m.Score.HasValue)
            .Select(m => m.Score!.Value)
            .DefaultIfEmpty(0f)
            .Average();

        var positiveUserMessages = userMessages
            .Count(m => m.Sentiment == "Positive");

        return new ConversationStats(history.Count, userMessages.Count, assistantMessages.Count, float.IsNaN((float)avgUserSentiment) ? 0f : (float)avgUserSentiment, positiveUserMessages, history.Count > 0
                ? (history.Last().Timestamp - history.First().Timestamp)
                : TimeSpan.Zero);
    }

    // Private helper methods

    private ChatMessage CreateUserMessage(string message)
    {
        var sentiment = _sentimentModel.Predict(message);
        return new ChatMessage
        {
            Role = "user",
            Message = message,
            Timestamp = DateTime.UtcNow,
            Sentiment = sentiment.Label,
            Score = sentiment.Score
        };
    }

    private ChatMessage CreateAssistantMessage(string message)
    {
        return new ChatMessage
        {
            Role = "assistant",
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

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
/// Represents sentiment analysis results.
/// </summary>
public class SentimentAnalysis
{
    public string Label { get; set; } = string.Empty;
    public float Score { get; set; }
    public bool IsPositive { get; set; }
}

/// <summary>
/// Represents conversation statistics.
/// </summary>
/// <param name="TotalMessages"></param>
/// <param name="UserMessageCount"></param>
/// <param name="AssistantMessageCount"></param>
/// <param name="AverageUserSentiment"></param>
/// <param name="PositiveUserMessageCount"></param>
/// <param name="SessionDuration"></param>
public record ConversationStats(int TotalMessages, int UserMessageCount, int AssistantMessageCount, float AverageUserSentiment, int PositiveUserMessageCount, TimeSpan SessionDuration);
