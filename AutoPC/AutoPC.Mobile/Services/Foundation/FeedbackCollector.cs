namespace AutoPC.Client.Services.Foundation;

/// <summary>
/// Collects and manages user feedback and ratings
/// Foundation for the learning and adaptation system
/// </summary>
public class FeedbackCollector
{
    private readonly StorageService _storageService;
    private readonly UserProfileService _userProfileService;
    private List<ResponseFeedback> _feedbackCache = new();
    private List<InteractionRecord> _interactionCache = new();
    private const string FEEDBACK_STORAGE_KEY = "response_feedback";
    private const string INTERACTIONS_STORAGE_KEY = "interaction_records";

    public FeedbackCollector(
        StorageService storageService,
        UserProfileService userProfileService)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
    }

    /// <summary>
    /// Initialize feedback system - load existing feedback
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("[Feedback] Initializing feedback system");

            // Load existing feedback
            var feedback = await _storageService.LoadAsync<List<ResponseFeedback>>(
                FEEDBACK_STORAGE_KEY,
                cancellationToken
            );
            _feedbackCache = feedback ?? new();

            // Load interaction records
            var interactions = await _storageService.LoadAsync<List<InteractionRecord>>(
                INTERACTIONS_STORAGE_KEY,
                cancellationToken
            );
            _interactionCache = interactions ?? new();

            Console.WriteLine($"[Feedback] Loaded {_feedbackCache.Count} feedback items");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Feedback] Error initializing: {ex.Message}");
        }
    }

    /// <summary>
    /// Submit feedback rating for a message
    /// </summary>
    public async Task<bool> SubmitFeedbackAsync(
        Guid messageId,
        int rating,
        string? comment = null,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (rating < 1 || rating > 5)
            {
                Console.WriteLine($"[Feedback] Invalid rating: {rating}. Must be 1-5.");
                return false;
            }

            var profile = _userProfileService.GetCurrentProfile();
            if (profile == null)
            {
                Console.WriteLine("[Feedback] No user profile found");
                return false;
            }

            var feedback = new ResponseFeedback
            {
                Id = Guid.NewGuid(),
                UserId = profile.Id,
                MessageId = messageId,
                Rating = rating,
                Comment = comment,
                Tags = tags ?? Array.Empty<string>(),
                CreatedAt = DateTime.UtcNow,
                IsHelpful = rating >= 4 // 4-5 stars considered helpful
            };

            _feedbackCache.Add(feedback);
            await SaveFeedbackAsync(cancellationToken);

            Console.WriteLine($"[Feedback] Submitted rating: {rating} stars for message {messageId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Feedback] Error submitting feedback: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Record an interaction for learning
    /// </summary>
    public async Task<bool> RecordInteractionAsync(
        string interactionType,
        string topic,
        string sentiment,
        int sentimentScore,
        long durationMs,
        bool wasSuccessful = true,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = _userProfileService.GetCurrentProfile();
            if (profile == null)
            {
                Console.WriteLine("[Feedback] No user profile found");
                return false;
            }

            var record = new InteractionRecord
            {
                Id = Guid.NewGuid(),
                UserId = profile.Id,
                InteractionType = interactionType,
                Topic = topic,
                Sentiment = sentiment,
                SentimentScore = sentimentScore,
                DurationMs = durationMs,
                WasSuccessful = wasSuccessful,
                Metadata = metadata ?? new(),
                CreatedAt = DateTime.UtcNow
            };

            _interactionCache.Add(record);
            await SaveInteractionsAsync(cancellationToken);

            Console.WriteLine($"[Feedback] Recorded interaction: {interactionType} ({durationMs}ms)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Feedback] Error recording interaction: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get average rating for a session
    /// </summary>
    public double GetAverageRating()
    {
        if (_feedbackCache.Count == 0)
            return 0;

        return _feedbackCache.Average(f => f.Rating);
    }

    /// <summary>
    /// Get feedback for a specific message
    /// </summary>
    public ResponseFeedback? GetFeedbackForMessage(Guid messageId)
    {
        return _feedbackCache.FirstOrDefault(f => f.MessageId == messageId);
    }

    /// <summary>
    /// Get all feedback
    /// </summary>
    public IReadOnlyList<ResponseFeedback> GetAllFeedback()
    {
        return _feedbackCache.AsReadOnly();
    }

    /// <summary>
    /// Get feedback statistics
    /// </summary>
    public FeedbackStatistics GetStatistics()
    {
        return new FeedbackStatistics
        {
            TotalFeedback = _feedbackCache.Count,
            AverageRating = GetAverageRating(),
            HelpfulCount = _feedbackCache.Count(f => f.IsHelpful),
            UnhelpfulCount = _feedbackCache.Count(f => !f.IsHelpful),
            LatestFeedbackTime = _feedbackCache.OrderByDescending(f => f.CreatedAt).FirstOrDefault()?.CreatedAt ?? DateTime.UtcNow,
            TotalInteractions = _interactionCache.Count,
            SuccessfulInteractions = _interactionCache.Count(i => i.WasSuccessful),
            AverageSentiment = _interactionCache.Count > 0
                ? _interactionCache.Average(i => i.SentimentScore)
                : 0
        };
    }

    /// <summary>
    /// Get interaction patterns
    /// </summary>
    public InteractionPatterns GetPatterns()
    {
        var patterns = new InteractionPatterns();

        // Analyze topics
        var topicCounts = _interactionCache
            .GroupBy(i => i.Topic)
            .OrderByDescending(g => g.Count())
            .Take(10);

        patterns.TopTopics = topicCounts
            .Select(g => new TopicPattern
            {
                Topic = g.Key,
                Frequency = g.Count(),
                AverageSentiment = (int)g.Average(i => i.SentimentScore)
            })
            .ToList();

        // Analyze sentiments
        patterns.SentimentDistribution = _interactionCache
            .GroupBy(i => i.Sentiment)
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate success rate
        if (_interactionCache.Count > 0)
        {
            patterns.SuccessRate = (double)_interactionCache.Count(i => i.WasSuccessful) / _interactionCache.Count;
        }

        return patterns;
    }

    /// <summary>
    /// Clear old feedback (for privacy/storage cleanup)
    /// </summary>
    public async Task<int> ClearOldFeedbackAsync(int daysOld = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var countBefore = _feedbackCache.Count;

            _feedbackCache = _feedbackCache
                .Where(f => f.CreatedAt > cutoffDate)
                .ToList();

            var removed = countBefore - _feedbackCache.Count;
            await SaveFeedbackAsync(cancellationToken);

            Console.WriteLine($"[Feedback] Removed {removed} old feedback items");
            return removed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Feedback] Error clearing old feedback: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Export feedback as JSON
    /// </summary>
    public string ExportAsJson()
    {
        var export = new
        {
            feedback = _feedbackCache,
            interactions = _interactionCache,
            statistics = GetStatistics()
        };

        return System.Text.Json.JsonSerializer.Serialize(
            export,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
        );
    }

    /// <summary>
    /// Save feedback to storage
    /// </summary>
    private async Task SaveFeedbackAsync(CancellationToken cancellationToken = default)
    {
        await _storageService.SaveAsync(FEEDBACK_STORAGE_KEY, _feedbackCache, cancellationToken);
    }

    /// <summary>
    /// Save interactions to storage
    /// </summary>
    private async Task SaveInteractionsAsync(CancellationToken cancellationToken = default)
    {
        await _storageService.SaveAsync(INTERACTIONS_STORAGE_KEY, _interactionCache, cancellationToken);
    }
}

/// <summary>
/// Statistics about user feedback
/// </summary>
public class FeedbackStatistics
{
    public int TotalFeedback { get; set; }
    public double AverageRating { get; set; }
    public int HelpfulCount { get; set; }
    public int UnhelpfulCount { get; set; }
    public DateTime LatestFeedbackTime { get; set; }
    public int TotalInteractions { get; set; }
    public int SuccessfulInteractions { get; set; }
    public double AverageSentiment { get; set; }
}

/// <summary>
/// Patterns discovered from interaction history
/// </summary>
public class InteractionPatterns
{
    public List<TopicPattern> TopTopics { get; set; } = new();
    public Dictionary<string, int> SentimentDistribution { get; set; } = new();
    public double SuccessRate { get; set; }
}

/// <summary>
/// Pattern of a topic
/// </summary>
public class TopicPattern
{
    public string Topic { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public int AverageSentiment { get; set; }
}
