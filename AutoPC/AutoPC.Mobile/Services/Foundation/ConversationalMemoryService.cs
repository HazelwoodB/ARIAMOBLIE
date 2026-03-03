namespace AutoPC.Client.Services.Foundation;

using System.Text;

/// <summary>
/// Conversational Memory Service - Maintains context and learns user patterns
/// Features: Topic tracking, preference learning, cultural context awareness
/// </summary>
public class ConversationalMemoryService
{
    private readonly StorageService _storage;
    private readonly PreferenceManager _preferenceManager;
    private readonly List<ConversationTurn> _currentSessionMemory = new();
    private const int MaxShortTermMemory = 20;
    private const string MemoryKey = "aria_conversational_memory";

    public ConversationalMemoryService(
        StorageService storage,
        PreferenceManager preferenceManager)
    {
        _storage = storage;
        _preferenceManager = preferenceManager;
    }

    /// <summary>
    /// Records a conversation turn and extracts learning patterns
    /// </summary>
    public async Task RecordConversationTurnAsync(
        string userMessage,
        string ariaResponse,
        EmotionAnalysis? emotion = null)
    {
        var turn = new ConversationTurn
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserMessage = userMessage,
            AriaResponse = ariaResponse,
            DetectedTopics = ExtractTopics(userMessage),
            UserEmotion = emotion?.PrimaryEmotion ?? "neutral",
            EmotionIntensity = emotion?.Intensity ?? 0,
            ConversationContext = BuildContext()
        };

        _currentSessionMemory.Add(turn);

        // Keep only recent turns in memory
        if (_currentSessionMemory.Count > MaxShortTermMemory)
        {
            _currentSessionMemory.RemoveAt(0);
        }

        // Save to persistent storage
        await SaveMemoryAsync(turn);

        // Learn from this interaction
        await LearnFromInteractionAsync(turn);
    }

    /// <summary>
    /// Retrieves relevant context from conversation history
    /// </summary>
    public async Task<ConversationContext> GetRelevantContextAsync(string currentQuery)
    {
        var queryTopics = ExtractTopics(currentQuery);
        var recentTurns = _currentSessionMemory.TakeLast(5).ToList();
        var persistentContext = await LoadRecentMemoryAsync();

        // Find related previous conversations
        var relatedTurns = FindRelatedTurns(queryTopics, persistentContext);

        return new ConversationContext
        {
            CurrentSessionTurns = recentTurns.Count,
            RecentTopics = GetRecentTopics(recentTurns),
            RelatedPreviousConversations = relatedTurns,
            UserPatterns = await GetLearnedPatternsAsync(),
            ConversationMood = AnalyzeConversationMood(recentTurns)
        };
    }

    /// <summary>
    /// Generates a contextual summary for LLM system prompt
    /// </summary>
    public async Task<string> GenerateContextSummaryAsync()
    {
        var context = await GetRelevantContextAsync("");
        
        if (context.CurrentSessionTurns == 0)
        {
            return "This is the beginning of a new conversation.";
        }

        var summary = new StringBuilder();
        summary.AppendLine($"Conversation context: {context.CurrentSessionTurns} turns in this session.");
        
        if (context.RecentTopics.Any())
        {
            summary.AppendLine($"Recent topics: {string.Join(", ", context.RecentTopics.Take(3))}");
        }

        if (context.UserPatterns.PreferredTopics.Any())
        {
            summary.AppendLine($"User's known interests: {string.Join(", ", context.UserPatterns.PreferredTopics.Take(5))}");
        }

        if (!string.IsNullOrEmpty(context.ConversationMood))
        {
            summary.AppendLine($"Conversation mood: {context.ConversationMood}");
        }

        return summary.ToString();
    }

    /// <summary>
    /// Checks if user might need a wellness check
    /// </summary>
    public async Task<WellnessCheck?> CheckUserWellnessAsync()
    {
        var preferences = _preferenceManager.GetCurrentPreferences();
        
        if (!preferences.EnableWellnessChecks)
        {
            return null;
        }

        var recentTurns = _currentSessionMemory.TakeLast(5).ToList();
        
        // Check for sustained negative emotion
        var negativeCount = recentTurns.Count(t => 
            t.UserEmotion is "sadness" or "anger" or "fear" && t.EmotionIntensity > 0.5f);

        if (negativeCount >= 3)
        {
            return new WellnessCheck
            {
                Trigger = "sustained_negative_emotion",
                Severity = "moderate",
                SuggestedResponse = "I've noticed you seem to be having a difficult time. Is there anything specific I can help with, or would you like to talk about it?"
            };
        }

        // Check for signs of frustration with ARIA
        var frustrationKeywords = new[] { "wrong", "incorrect", "doesn't work", "not helping", "useless" };
        var frustrationCount = recentTurns.Count(t => 
            frustrationKeywords.Any(k => t.UserMessage.Contains(k, StringComparison.OrdinalIgnoreCase)));

        if (frustrationCount >= 2)
        {
            return new WellnessCheck
            {
                Trigger = "user_frustration",
                Severity = "low",
                SuggestedResponse = "I sense some frustration. Let me try a different approach - can you help me understand what would be most helpful right now?"
            };
        }

        return await Task.FromResult<WellnessCheck?>(null);
    }

    #region Private Methods

    private List<string> ExtractTopics(string message)
    {
        var topics = new List<string>();
        var lowerMessage = message.ToLower();

        // Technology topics
        if (lowerMessage.Contains("code") || lowerMessage.Contains("programming") || lowerMessage.Contains("software"))
            topics.Add("programming");
        if (lowerMessage.Contains("ai") || lowerMessage.Contains("machine learning") || lowerMessage.Contains("llm"))
            topics.Add("artificial-intelligence");
        if (lowerMessage.Contains("web") || lowerMessage.Contains("html") || lowerMessage.Contains("css"))
            topics.Add("web-development");
        if (lowerMessage.Contains("database") || lowerMessage.Contains("sql"))
            topics.Add("database");
        if (lowerMessage.Contains("design") || lowerMessage.Contains("ui") || lowerMessage.Contains("ux"))
            topics.Add("design");

        // Personal topics
        if (lowerMessage.Contains("help") || lowerMessage.Contains("problem") || lowerMessage.Contains("issue"))
            topics.Add("problem-solving");
        if (lowerMessage.Contains("learn") || lowerMessage.Contains("understand") || lowerMessage.Contains("explain"))
            topics.Add("learning");
        if (lowerMessage.Contains("career") || lowerMessage.Contains("job") || lowerMessage.Contains("work"))
            topics.Add("career");

        return topics;
    }

    private string BuildContext()
    {
        if (_currentSessionMemory.Count == 0)
        {
            return "new_conversation";
        }

        var recentTopics = GetRecentTopics(_currentSessionMemory.TakeLast(3).ToList());
        return string.Join(", ", recentTopics);
    }

    private List<string> GetRecentTopics(List<ConversationTurn> turns)
    {
        return turns
            .SelectMany(t => t.DetectedTopics)
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(5)
            .ToList();
    }

    private List<ConversationTurn> FindRelatedTurns(List<string> queryTopics, List<ConversationTurn> history)
    {
        return history
            .Where(t => t.DetectedTopics.Any(topic => queryTopics.Contains(topic)))
            .OrderByDescending(t => t.Timestamp)
            .Take(3)
            .ToList();
    }

    private string AnalyzeConversationMood(List<ConversationTurn> turns)
    {
        if (turns.Count == 0) return "neutral";

        var avgIntensity = turns.Average(t => t.EmotionIntensity);
        var dominantEmotion = turns
            .GroupBy(t => t.UserEmotion)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "neutral";

        if (avgIntensity > 0.6f)
        {
            return $"highly-{dominantEmotion}";
        }
        else if (avgIntensity > 0.3f)
        {
            return $"moderately-{dominantEmotion}";
        }

        return dominantEmotion;
    }

    private async Task LearnFromInteractionAsync(ConversationTurn turn)
    {
        var patterns = await GetLearnedPatternsAsync();

        // Track preferred topics
        foreach (var topic in turn.DetectedTopics)
        {
            if (patterns.PreferredTopics.ContainsKey(topic))
            {
                patterns.PreferredTopics[topic]++;
            }
            else
            {
                patterns.PreferredTopics[topic] = 1;
            }
        }

        // Track communication patterns
        if (turn.UserMessage.Length < 50)
        {
            patterns.PrefersBriefMessages = true;
        }
        else if (turn.UserMessage.Length > 200)
        {
            patterns.PrefersDetailedMessages = true;
        }

        // Track time patterns
        var hour = turn.Timestamp.Hour;
        if (!patterns.ActiveHours.Contains(hour))
        {
            patterns.ActiveHours.Add(hour);
        }

        await SaveLearnedPatternsAsync(patterns);
    }

    private async Task<UserPatterns> GetLearnedPatternsAsync()
    {
        var patterns = await _storage.LoadAsync<UserPatterns>("aria_user_patterns");
        return patterns ?? new UserPatterns();
    }

    private async Task SaveLearnedPatternsAsync(UserPatterns patterns)
    {
        await _storage.SaveAsync("aria_user_patterns", patterns);
    }

    private async Task SaveMemoryAsync(ConversationTurn turn)
    {
        var history = await LoadRecentMemoryAsync();
        history.Add(turn);

        // Keep only last 100 turns in persistent storage
        if (history.Count > 100)
        {
            history = history.TakeLast(100).ToList();
        }

        await _storage.SaveAsync(MemoryKey, history);
    }

    private async Task<List<ConversationTurn>> LoadRecentMemoryAsync()
    {
        var history = await _storage.LoadAsync<List<ConversationTurn>>(MemoryKey);
        return history ?? new List<ConversationTurn>();
    }

    #endregion
}

#region Supporting Models

public class ConversationTurn
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public string AriaResponse { get; set; } = string.Empty;
    public List<string> DetectedTopics { get; set; } = new();
    public string UserEmotion { get; set; } = "neutral";
    public float EmotionIntensity { get; set; }
    public string ConversationContext { get; set; } = string.Empty;
}

public class ConversationContext
{
    public int CurrentSessionTurns { get; set; }
    public List<string> RecentTopics { get; set; } = new();
    public List<ConversationTurn> RelatedPreviousConversations { get; set; } = new();
    public UserPatterns UserPatterns { get; set; } = new();
    public string ConversationMood { get; set; } = "neutral";
}

public class UserPatterns
{
    public Dictionary<string, int> PreferredTopics { get; set; } = new();
    public bool PrefersBriefMessages { get; set; }
    public bool PrefersDetailedMessages { get; set; }
    public List<int> ActiveHours { get; set; } = new();
    public string PreferredCommunicationStyle { get; set; } = "casual";
}

public class WellnessCheck
{
    public string Trigger { get; set; } = string.Empty;
    public string Severity { get; set; } = "low";
    public string SuggestedResponse { get; set; } = string.Empty;
}

#endregion
