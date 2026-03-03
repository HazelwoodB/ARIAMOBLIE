namespace AutoPC.Client.Services.Foundation;

using System.Text;

/// <summary>
/// Feedback Learning Service - Analyzes user feedback to improve LLM responses.
/// Generates dynamic system prompt adjustments based on what users rate highly vs poorly.
/// This creates a closed-loop learning system: User Feedback -> Analysis -> Prompt Tuning -> Better Responses.
/// </summary>
public class FeedbackLearningService
{
    private readonly FeedbackCollector _feedbackCollector;
    private readonly StorageService _storageService;
    private const string LEARNING_DATA_KEY = "feedback_learning_data";
    private const string PROMPT_ADJUSTMENTS_KEY = "prompt_adjustments";

    private LearningData _learningData = new();
    private bool _initialized = false;

    public FeedbackLearningService(
        FeedbackCollector feedbackCollector,
        StorageService storageService)
    {
        _feedbackCollector = feedbackCollector ?? throw new ArgumentNullException(nameof(feedbackCollector));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    }

    /// <summary>
    /// Initialize the learning service - load previous learning data
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("[Learning] Initializing feedback learning service");

            var data = await _storageService.LoadAsync<LearningData>(LEARNING_DATA_KEY, cancellationToken);
            _learningData = data ?? new LearningData();
            _initialized = true;

            Console.WriteLine($"[Learning] Loaded {_learningData.FeedbackPatterns.Count} learned patterns, " +
                              $"{_learningData.PositiveTraits.Count} positive traits, " +
                              $"{_learningData.NegativeTraits.Count} negative traits");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Learning] Initialization error: {ex.Message}");
            _learningData = new LearningData();
            _initialized = true;
        }
    }

    /// <summary>
    /// Process new feedback and update learning model.
    /// Called after each user feedback submission.
    /// </summary>
    public async Task ProcessFeedbackAsync(
        Guid messageId,
        int rating,
        string? comment,
        string userMessage,
        string assistantResponse,
        CancellationToken cancellationToken = default)
    {
        if (!_initialized) await InitializeAsync(cancellationToken);

        try
        {
            Console.WriteLine($"[Learning] Processing feedback: rating={rating}, messageId={messageId}");

            // Extract features from the interaction
            var features = ExtractInteractionFeatures(userMessage, assistantResponse);

            // Record the pattern
            var pattern = new FeedbackPattern
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                Rating = rating,
                Comment = comment,
                UserQueryType = features.QueryType,
                ResponseLength = assistantResponse.Length,
                ResponseTone = features.DetectedTone,
                Topics = features.Topics,
                HadCodeBlock = features.HasCodeBlock,
                HadEmoji = features.HasEmoji,
                HadList = features.HasList,
                CreatedAt = DateTime.UtcNow
            };

            _learningData.FeedbackPatterns.Add(pattern);

            // Keep only last 200 patterns to manage storage size
            if (_learningData.FeedbackPatterns.Count > 200)
            {
                _learningData.FeedbackPatterns = _learningData.FeedbackPatterns
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(200)
                    .ToList();
            }

            // Analyze and update learned traits
            UpdateLearnedTraits(pattern, features);

            // Record comment keywords for specific improvement areas
            if (!string.IsNullOrWhiteSpace(comment))
            {
                ExtractImprovementHints(comment, rating);
            }

            // Update response length preference
            UpdateLengthPreference(assistantResponse.Length, rating);

            // Persist learning data
            _learningData.LastUpdated = DateTime.UtcNow;
            _learningData.TotalFeedbackProcessed++;
            await _storageService.SaveAsync(LEARNING_DATA_KEY, _learningData, cancellationToken);

            Console.WriteLine($"[Learning] Feedback processed. Total patterns: {_learningData.FeedbackPatterns.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Learning] Error processing feedback: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate a dynamic system prompt supplement based on learned feedback patterns.
    /// This is appended to the base ARIA system prompt to guide better responses.
    /// </summary>
    public string GeneratePromptAdjustment()
    {
        if (_learningData.FeedbackPatterns.Count < 3)
        {
            return string.Empty; // Need minimum feedback before adjusting
        }

        var adjustment = new StringBuilder();
        adjustment.AppendLine("\n[LEARNED USER PREFERENCES - Based on feedback analysis]");

        // Response length preference
        var lengthPref = AnalyzeLengthPreference();
        if (!string.IsNullOrEmpty(lengthPref))
        {
            adjustment.AppendLine($"- Preferred response length: {lengthPref}");
        }

        // Positive traits (things users like)
        var topPositive = _learningData.PositiveTraits
            .OrderByDescending(t => t.Value)
            .Take(5)
            .Where(t => t.Value >= 2)
            .ToList();

        if (topPositive.Any())
        {
            adjustment.AppendLine("- Users respond well to: " + string.Join(", ", topPositive.Select(t => t.Key)));
        }

        // Negative traits (things to avoid)
        var topNegative = _learningData.NegativeTraits
            .OrderByDescending(t => t.Value)
            .Take(5)
            .Where(t => t.Value >= 2)
            .ToList();

        if (topNegative.Any())
        {
            adjustment.AppendLine("- Avoid: " + string.Join(", ", topNegative.Select(t => t.Key)));
        }

        // Improvement hints from user comments
        var recentHints = _learningData.ImprovementHints
            .OrderByDescending(h => h.Frequency)
            .Take(3)
            .ToList();

        if (recentHints.Any())
        {
            adjustment.AppendLine("- Specific improvement areas: " + string.Join(", ", recentHints.Select(h => h.Hint)));
        }

        // Topic-specific preferences
        var topicPrefs = AnalyzeTopicPreferences();
        if (topicPrefs.Any())
        {
            adjustment.AppendLine("- Topic preferences: " + string.Join("; ", topicPrefs.Take(3)));
        }

        // Tone preference
        var tonePref = AnalyzeTonePreference();
        if (!string.IsNullOrEmpty(tonePref))
        {
            adjustment.AppendLine($"- Preferred tone: {tonePref}");
        }

        var result = adjustment.ToString();
        return result.Length > 50 ? result : string.Empty; // Only return if meaningful
    }

    /// <summary>
    /// Get the overall learning effectiveness score (0-100)
    /// </summary>
    public int GetLearningScore()
    {
        if (_learningData.FeedbackPatterns.Count == 0)
            return 0;

        var recentPatterns = _learningData.FeedbackPatterns
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToList();

        if (recentPatterns.Count == 0)
            return 0;

        var avgRating = recentPatterns.Average(p => p.Rating);
        return (int)Math.Min(100, (avgRating / 5.0) * 100);
    }

    /// <summary>
    /// Get a summary of what the system has learned
    /// </summary>
    public LearningSummary GetLearningSummary()
    {
        var recentPatterns = _learningData.FeedbackPatterns
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .ToList();

        return new LearningSummary
        {
            TotalFeedbackProcessed = _learningData.TotalFeedbackProcessed,
            AverageRating = recentPatterns.Count > 0 ? recentPatterns.Average(p => p.Rating) : 0,
            LearningScore = GetLearningScore(),
            PositiveTraits = _learningData.PositiveTraits
                .OrderByDescending(t => t.Value)
                .Take(5)
                .Select(t => t.Key)
                .ToList(),
            NegativeTraits = _learningData.NegativeTraits
                .OrderByDescending(t => t.Value)
                .Take(5)
                .Select(t => t.Key)
                .ToList(),
            TopImprovementHints = _learningData.ImprovementHints
                .OrderByDescending(h => h.Frequency)
                .Take(5)
                .Select(h => h.Hint)
                .ToList(),
            LastUpdated = _learningData.LastUpdated
        };
    }

    #region Private Analysis Methods

    private InteractionFeatures ExtractInteractionFeatures(string userMessage, string assistantResponse)
    {
        var lowerUser = userMessage.ToLower();
        var lowerResponse = assistantResponse.ToLower();

        return new InteractionFeatures
        {
            QueryType = ClassifyQuery(lowerUser),
            DetectedTone = DetectTone(lowerResponse),
            Topics = ExtractTopics(lowerUser),
            HasCodeBlock = assistantResponse.Contains("```") || assistantResponse.Contains("    "),
            HasEmoji = ContainsEmoji(assistantResponse),
            HasList = assistantResponse.Contains("\n-") || assistantResponse.Contains("\n•") || assistantResponse.Contains("\n1.")
        };
    }

    private string ClassifyQuery(string message)
    {
        if (message.Contains("how") || message.Contains("explain") || message.Contains("what is"))
            return "explanation";
        if (message.Contains("fix") || message.Contains("error") || message.Contains("bug") || message.Contains("problem"))
            return "troubleshooting";
        if (message.Contains("write") || message.Contains("create") || message.Contains("generate") || message.Contains("code"))
            return "generation";
        if (message.Contains("?"))
            return "question";
        if (message.Contains("help"))
            return "help-request";
        return "general";
    }

    private string DetectTone(string response)
    {
        var casualMarkers = new[] { "hey", "gonna", "kinda", "pretty much", "cool", "awesome", "!" };
        var formalMarkers = new[] { "therefore", "consequently", "furthermore", "regarding", "pursuant" };
        var technicalMarkers = new[] { "algorithm", "implementation", "parameter", "function", "module", "api" };
        var friendlyMarkers = new[] { "glad", "happy to help", "great question", "absolutely", "of course" };

        int casual = casualMarkers.Count(m => response.Contains(m));
        int formal = formalMarkers.Count(m => response.Contains(m));
        int technical = technicalMarkers.Count(m => response.Contains(m));
        int friendly = friendlyMarkers.Count(m => response.Contains(m));

        var max = new[] { ("casual", casual), ("formal", formal), ("technical", technical), ("friendly", friendly) }
            .OrderByDescending(x => x.Item2)
            .First();

        return max.Item2 > 0 ? max.Item1 : "neutral";
    }

    private List<string> ExtractTopics(string message)
    {
        var topics = new List<string>();
        var topicKeywords = new Dictionary<string, string[]>
        {
            ["programming"] = ["code", "programming", "software", "develop", "debug"],
            ["ai-ml"] = ["ai", "machine learning", "model", "neural", "training"],
            ["web"] = ["web", "html", "css", "javascript", "frontend", "backend"],
            ["data"] = ["database", "sql", "data", "query", "table"],
            ["general"] = ["help", "question", "explain", "tell me"]
        };

        foreach (var (topic, keywords) in topicKeywords)
        {
            if (keywords.Any(k => message.Contains(k)))
                topics.Add(topic);
        }

        return topics.Any() ? topics : new List<string> { "general" };
    }

    private bool ContainsEmoji(string text)
    {
        // Check for common emoji patterns
        return text.Any(c => char.IsHighSurrogate(c)) ||
               text.Contains("⭐") || text.Contains("✓") || text.Contains("✗") ||
               text.Contains("⚠️") || text.Contains("💡") || text.Contains("🔧");
    }

    private void UpdateLearnedTraits(FeedbackPattern pattern, InteractionFeatures features)
    {
        bool isPositive = pattern.Rating >= 4;
        bool isNegative = pattern.Rating <= 2;

        var traits = isPositive ? _learningData.PositiveTraits : 
                     isNegative ? _learningData.NegativeTraits : null;

        if (traits == null) return; // Neutral rating (3) doesn't strongly indicate preference

        // Response format traits
        if (pattern.HadCodeBlock)
            IncrementTrait(traits, "code examples");
        if (pattern.HadEmoji)
            IncrementTrait(traits, "emoji usage");
        if (pattern.HadList)
            IncrementTrait(traits, "structured lists");

        // Tone traits
        if (!string.IsNullOrEmpty(features.DetectedTone))
            IncrementTrait(traits, $"{features.DetectedTone} tone");

        // Length traits
        if (pattern.ResponseLength < 200)
            IncrementTrait(traits, "brief responses");
        else if (pattern.ResponseLength > 800)
            IncrementTrait(traits, "detailed responses");
        else
            IncrementTrait(traits, "moderate-length responses");

        // Query type traits
        IncrementTrait(traits, $"{features.QueryType} responses");
    }

    private void IncrementTrait(Dictionary<string, int> traits, string trait)
    {
        if (traits.ContainsKey(trait))
            traits[trait]++;
        else
            traits[trait] = 1;
    }

    private void ExtractImprovementHints(string comment, int rating)
    {
        var lowerComment = comment.ToLower();
        var hintKeywords = new Dictionary<string, string>
        {
            ["too long"] = "shorter responses",
            ["too short"] = "more detailed responses",
            ["too technical"] = "simpler language",
            ["too simple"] = "more technical depth",
            ["wrong"] = "accuracy improvement",
            ["incorrect"] = "accuracy improvement",
            ["not helpful"] = "relevance improvement",
            ["confusing"] = "clearer explanations",
            ["more examples"] = "include more examples",
            ["code"] = "include code examples",
            ["faster"] = "more concise answers",
            ["generic"] = "more specific/personalized answers",
            ["outdated"] = "up-to-date information",
            ["rude"] = "more polite tone",
            ["boring"] = "more engaging responses"
        };

        foreach (var (keyword, hint) in hintKeywords)
        {
            if (lowerComment.Contains(keyword))
            {
                var existing = _learningData.ImprovementHints.FirstOrDefault(h => h.Hint == hint);
                if (existing != null)
                {
                    existing.Frequency++;
                    existing.LastSeen = DateTime.UtcNow;
                }
                else
                {
                    _learningData.ImprovementHints.Add(new ImprovementHint
                    {
                        Hint = hint,
                        Frequency = 1,
                        LastSeen = DateTime.UtcNow,
                        SourceRating = rating
                    });
                }
            }
        }
    }

    private void UpdateLengthPreference(int length, int rating)
    {
        _learningData.LengthRatings.Add(new LengthRating
        {
            Length = length,
            Rating = rating,
            CreatedAt = DateTime.UtcNow
        });

        // Keep only recent 100 length ratings
        if (_learningData.LengthRatings.Count > 100)
        {
            _learningData.LengthRatings = _learningData.LengthRatings
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToList();
        }
    }

    private string? AnalyzeLengthPreference()
    {
        if (_learningData.LengthRatings.Count < 5)
            return null;

        var highRated = _learningData.LengthRatings.Where(l => l.Rating >= 4).ToList();
        if (highRated.Count == 0) return null;

        var avgLength = highRated.Average(l => l.Length);
        return avgLength switch
        {
            < 200 => "brief and concise (under 200 chars)",
            < 500 => "moderate length (200-500 chars)",
            < 1000 => "detailed (500-1000 chars)",
            _ => "very detailed (1000+ chars)"
        };
    }

    private List<string> AnalyzeTopicPreferences()
    {
        var topicRatings = _learningData.FeedbackPatterns
            .Where(p => p.Topics.Any())
            .SelectMany(p => p.Topics.Select(t => new { Topic = t, p.Rating }))
            .GroupBy(x => x.Topic)
            .Where(g => g.Count() >= 2)
            .Select(g => new { Topic = g.Key, AvgRating = g.Average(x => x.Rating), Count = g.Count() })
            .OrderByDescending(x => x.AvgRating)
            .ToList();

        return topicRatings
            .Select(t => $"{t.Topic}: avg {t.AvgRating:F1}★ ({t.Count} ratings)")
            .ToList();
    }

    private string? AnalyzeTonePreference()
    {
        var toneRatings = _learningData.FeedbackPatterns
            .Where(p => !string.IsNullOrEmpty(p.ResponseTone))
            .GroupBy(p => p.ResponseTone)
            .Where(g => g.Count() >= 2)
            .Select(g => new { Tone = g.Key, AvgRating = g.Average(p => p.Rating) })
            .OrderByDescending(x => x.AvgRating)
            .FirstOrDefault();

        return toneRatings != null ? $"{toneRatings.Tone} (avg {toneRatings.AvgRating:F1}★)" : null;
    }

    #endregion
}

#region Data Models

/// <summary>
/// Persisted learning data - accumulated from all user feedback
/// </summary>
public class LearningData
{
    public List<FeedbackPattern> FeedbackPatterns { get; set; } = new();
    public Dictionary<string, int> PositiveTraits { get; set; } = new();
    public Dictionary<string, int> NegativeTraits { get; set; } = new();
    public List<ImprovementHint> ImprovementHints { get; set; } = new();
    public List<LengthRating> LengthRatings { get; set; } = new();
    public int TotalFeedbackProcessed { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A single feedback pattern with extracted features
/// </summary>
public class FeedbackPattern
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string UserQueryType { get; set; } = string.Empty;
    public int ResponseLength { get; set; }
    public string ResponseTone { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = new();
    public bool HadCodeBlock { get; set; }
    public bool HadEmoji { get; set; }
    public bool HadList { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// An improvement hint extracted from user comments
/// </summary>
public class ImprovementHint
{
    public string Hint { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public DateTime LastSeen { get; set; }
    public int SourceRating { get; set; }
}

/// <summary>
/// Response length vs rating data point
/// </summary>
public class LengthRating
{
    public int Length { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Features extracted from a user-assistant interaction
/// </summary>
public class InteractionFeatures
{
    public string QueryType { get; set; } = string.Empty;
    public string DetectedTone { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = new();
    public bool HasCodeBlock { get; set; }
    public bool HasEmoji { get; set; }
    public bool HasList { get; set; }
}

/// <summary>
/// Summary of what the learning system has learned
/// </summary>
public class LearningSummary
{
    public int TotalFeedbackProcessed { get; set; }
    public double AverageRating { get; set; }
    public int LearningScore { get; set; }
    public List<string> PositiveTraits { get; set; } = new();
    public List<string> NegativeTraits { get; set; } = new();
    public List<string> TopImprovementHints { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

#endregion
