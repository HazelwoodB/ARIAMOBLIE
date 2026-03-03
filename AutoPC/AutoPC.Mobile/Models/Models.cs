namespace AutoPC.Client;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Sentiment { get; set; }
    public float? Score { get; set; }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    public float Score { get; set; }
}

/// <summary>
/// User profile - stores user-specific data and preferences
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "User";
    public string? DisplayName { get; set; }
    public string? TimeZone { get; set; }
    public string? Locale { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// User preferences - customizable settings for ARIA's behavior
/// </summary>
public class UserPreferences
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    
    // Communication Style
    public string CommunicationStyle { get; set; } = "casual"; // casual, formal, technical
    public int ResponseLength { get; set; } = 2; // 1=brief, 2=normal, 3=detailed
    public bool EnableEmojis { get; set; } = true;
    public bool EnableHumor { get; set; } = true;
    
    // Interaction Preferences
    public string[] Topics { get; set; } = Array.Empty<string>();
    public bool EnableGreetings { get; set; } = true;
    public bool EnableWellnessChecks { get; set; } = true;
    
    // Privacy & Data
    public bool SaveConversationHistory { get; set; } = true;
    public int MaxHistoryDays { get; set; } = 30;
    public bool AnonymizeData { get; set; } = false;
    
    // System Behavior
    public bool EnableContextAwareness { get; set; } = true;
    public bool EnableProactiveAlerts { get; set; } = true;
    public int AlertSensitivity { get; set; } = 5; // 1-10 scale
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// User feedback on responses - foundation for learning system
/// </summary>
public class ResponseFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid MessageId { get; set; }
    public int Rating { get; set; } // 1-5 stars
    public string? Comment { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsHelpful { get; set; }
}

/// <summary>
/// Interaction record - stores pattern data for learning
/// </summary>
public class InteractionRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string InteractionType { get; set; } = string.Empty; // "query", "feedback", "rating"
    public string Topic { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    public int SentimentScore { get; set; }
    public long DurationMs { get; set; }
    public bool WasSuccessful { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
