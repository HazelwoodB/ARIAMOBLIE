namespace AutoPC.Models;

/// <summary>
/// Server-side feedback entry - persisted to SQL database.
/// Stores user ratings, comments, and associated message context for ML retraining.
/// </summary>
public class FeedbackEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid MessageId { get; set; }
    public int Rating { get; set; } // 1-5 stars
    public string? Comment { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool IsHelpful { get; set; } // Rating >= 4
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Context fields for ML retraining
    public string UserQuery { get; set; } = string.Empty; // The user message that prompted the response
    public string AssistantResponse { get; set; } = string.Empty; // The assistant response that was rated
    public string? DetectedSentiment { get; set; } // Sentiment of the user query at time of interaction
    public float? SentimentScore { get; set; }

    // Serialized tags for DB storage (EF doesn't natively support string[])
    public string TagsSerialized
    {
        get => string.Join(",", Tags);
        set => Tags = string.IsNullOrEmpty(value) ? Array.Empty<string>() : value.Split(',');
    }
}

/// <summary>
/// Aggregated feedback statistics returned by the API.
/// </summary>
public class FeedbackStatsResponse
{
    public int TotalFeedback { get; set; }
    public double AverageRating { get; set; }
    public int HelpfulCount { get; set; }
    public int UnhelpfulCount { get; set; }
    public Dictionary<int, int> RatingBreakdown { get; set; } = new();
    public int TotalTrainingSamples { get; set; }
    public DateTime? LastRetrainedAt { get; set; }
    public double ModelAccuracy { get; set; }
    public DateTime? LatestFeedbackAt { get; set; }
}

/// <summary>
/// Request model for submitting feedback from the client.
/// </summary>
public class FeedbackSubmitRequest
{
    public Guid UserId { get; set; }
    public Guid MessageId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string[]? Tags { get; set; }
    public string UserQuery { get; set; } = string.Empty;
    public string AssistantResponse { get; set; } = string.Empty;
    public string? DetectedSentiment { get; set; }
    public float? SentimentScore { get; set; }
}
