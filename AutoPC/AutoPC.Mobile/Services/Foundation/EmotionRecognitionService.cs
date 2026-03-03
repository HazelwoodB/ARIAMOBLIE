namespace AutoPC.Client.Services.Foundation;

using AutoPC.Client.Services;

/// <summary>
/// Enhanced emotion recognition beyond basic sentiment analysis
/// Detects nuanced emotions: joy, sadness, anger, fear, surprise, trust, anticipation
/// Uses multi-dimensional emotion model (Plutchik's wheel of emotions)
/// </summary>
public class EmotionRecognitionService
{
    private readonly ClientSentimentService _sentimentService;

    public EmotionRecognitionService(ClientSentimentService sentimentService)
    {
        _sentimentService = sentimentService;
    }

    /// <summary>
    /// Analyzes text and returns primary emotion with intensity
    /// </summary>
    public EmotionAnalysis AnalyzeEmotion(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new EmotionAnalysis
            {
                PrimaryEmotion = "neutral",
                Intensity = 0.0f,
                SecondaryEmotions = new Dictionary<string, float>()
            };
        }

        var lowerText = text.ToLower();
        var emotions = new Dictionary<string, float>();

        // Joy indicators
        emotions["joy"] = CalculateEmotionScore(lowerText, JoyKeywords);
        
        // Sadness indicators
        emotions["sadness"] = CalculateEmotionScore(lowerText, SadnessKeywords);
        
        // Anger indicators
        emotions["anger"] = CalculateEmotionScore(lowerText, AngerKeywords);
        
        // Fear/Anxiety indicators
        emotions["fear"] = CalculateEmotionScore(lowerText, FearKeywords);
        
        // Surprise indicators
        emotions["surprise"] = CalculateEmotionScore(lowerText, SurpriseKeywords);
        
        // Trust/Confidence indicators
        emotions["trust"] = CalculateEmotionScore(lowerText, TrustKeywords);
        
        // Anticipation/Excitement indicators
        emotions["anticipation"] = CalculateEmotionScore(lowerText, AnticipationKeywords);
        
        // Disgust indicators
        emotions["disgust"] = CalculateEmotionScore(lowerText, DisgustKeywords);

        // Combine with sentiment analysis for more accurate results
        var sentiment = _sentimentService.AnalyzeSentiment(text);
        AdjustEmotionsBasedOnSentiment(emotions, sentiment.Label, sentiment.Score);

        // Find primary emotion
        var primaryEmotion = emotions.OrderByDescending(e => e.Value).FirstOrDefault();
        var secondaryEmotions = emotions
            .Where(e => e.Key != primaryEmotion.Key && e.Value > 0.2f)
            .OrderByDescending(e => e.Value)
            .Take(2)
            .ToDictionary(e => e.Key, e => e.Value);

        return new EmotionAnalysis
        {
            PrimaryEmotion = primaryEmotion.Key ?? "neutral",
            Intensity = primaryEmotion.Value,
            SecondaryEmotions = secondaryEmotions,
            Sentiment = sentiment.Label,
            SentimentScore = sentiment.Score
        };
    }

    /// <summary>
    /// Generates appropriate empathetic response based on detected emotion
    /// </summary>
    public string GenerateEmpatheticResponse(EmotionAnalysis emotion)
    {
        return emotion.PrimaryEmotion switch
        {
            "joy" => GetRandomResponse(JoyResponses),
            "sadness" => GetRandomResponse(SadnessResponses),
            "anger" => GetRandomResponse(AngerResponses),
            "fear" => GetRandomResponse(FearResponses),
            "surprise" => GetRandomResponse(SurpriseResponses),
            "trust" => GetRandomResponse(TrustResponses),
            "anticipation" => GetRandomResponse(AnticipationResponses),
            "disgust" => GetRandomResponse(DisgustResponses),
            _ => "I understand."
        };
    }

    private float CalculateEmotionScore(string text, string[] keywords)
    {
        var matches = keywords.Count(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        var baseScore = Math.Min(matches / (float)keywords.Length, 1.0f);
        
        // Boost score based on punctuation intensity
        if (text.Contains('!')) baseScore *= 1.2f;
        if (text.Contains("!!")) baseScore *= 1.3f;
        if (text.Contains('?')) baseScore *= 1.1f;
        
        return Math.Min(baseScore, 1.0f);
    }

    private void AdjustEmotionsBasedOnSentiment(Dictionary<string, float> emotions, string sentiment, float score)
    {
        switch (sentiment.ToLower())
        {
            case "positive":
                emotions["joy"] = Math.Max(emotions["joy"], score * 0.7f);
                emotions["sadness"] *= 0.5f;
                emotions["anger"] *= 0.5f;
                break;
            case "negative":
                emotions["sadness"] = Math.Max(emotions["sadness"], score * 0.5f);
                emotions["anger"] = Math.Max(emotions["anger"], score * 0.4f);
                emotions["joy"] *= 0.5f;
                break;
            case "neutral":
                emotions["trust"] = Math.Max(emotions["trust"], 0.3f);
                break;
        }
    }

    private string GetRandomResponse(string[] responses)
    {
        var random = new Random();
        return responses[random.Next(responses.Length)];
    }

    #region Emotion Keywords

    private static readonly string[] JoyKeywords = new[]
    {
        "happy", "joy", "excited", "wonderful", "amazing", "fantastic", "great", "awesome",
        "love", "enjoy", "pleased", "delighted", "cheerful", "glad", "thrilled", "ecstatic"
    };

    private static readonly string[] SadnessKeywords = new[]
    {
        "sad", "unhappy", "depressed", "down", "upset", "disappointed", "hurt", "lonely",
        "miserable", "gloomy", "heartbroken", "devastated", "sorry", "regret", "miss"
    };

    private static readonly string[] AngerKeywords = new[]
    {
        "angry", "mad", "furious", "irritated", "annoyed", "frustrated", "rage", "hate",
        "outraged", "infuriated", "pissed", "bitter", "resentful", "hostile"
    };

    private static readonly string[] FearKeywords = new[]
    {
        "afraid", "scared", "fear", "worried", "anxious", "nervous", "terrified", "panic",
        "frightened", "alarmed", "concern", "dread", "uneasy", "stress", "overwhelm"
    };

    private static readonly string[] SurpriseKeywords = new[]
    {
        "surprise", "shocked", "amazed", "astonished", "wow", "unexpected", "stunned",
        "startled", "incredible", "unbelievable", "omg", "whoa"
    };

    private static readonly string[] TrustKeywords = new[]
    {
        "trust", "believe", "confident", "sure", "certain", "rely", "depend", "faith",
        "comfortable", "secure", "safe", "reassure", "count on"
    };

    private static readonly string[] AnticipationKeywords = new[]
    {
        "anticipate", "expect", "hope", "looking forward", "can't wait", "eager", "excited",
        "upcoming", "soon", "planning", "preparing", "future", "tomorrow"
    };

    private static readonly string[] DisgustKeywords = new[]
    {
        "disgust", "gross", "sick", "nasty", "awful", "terrible", "revolting", "repulsive",
        "yuck", "eww", "hate", "loathe", "detest"
    };

    #endregion

    #region Empathetic Responses

    private static readonly string[] JoyResponses = new[]
    {
        "I'm so glad to hear that! Your enthusiasm is infectious.",
        "That's wonderful! It sounds like things are going well for you.",
        "I can sense your excitement! This is fantastic news.",
        "Your joy is palpable! I'm delighted to share in this moment with you."
    };

    private static readonly string[] SadnessResponses = new[]
    {
        "I hear you, and I'm here for you. It's okay to feel this way.",
        "That sounds difficult. I'm listening, and I understand.",
        "I'm sorry you're going through this. Your feelings are valid.",
        "I can sense this is weighing on you. Take your time, I'm here."
    };

    private static readonly string[] AngerResponses = new[]
    {
        "I understand your frustration. Let's work through this together.",
        "That does sound irritating. Your reaction is completely understandable.",
        "I can tell this has really gotten to you. Let's see how we can address it.",
        "I hear the intensity in your words. Sometimes things just need to be said."
    };

    private static readonly string[] FearResponses = new[]
    {
        "I understand this feels overwhelming. Let's take it one step at a time.",
        "It's natural to feel anxious about this. You're not alone in this.",
        "I can sense your concern. Let's break this down into manageable pieces.",
        "Your worries are valid. Together, we can work through this."
    };

    private static readonly string[] SurpriseResponses = new[]
    {
        "Wow, that is unexpected! Tell me more.",
        "I can feel the surprise in your message! This is quite something.",
        "That's certainly a plot twist! How are you processing this?",
        "What a revelation! I'm curious to hear how this unfolded."
    };

    private static readonly string[] TrustResponses = new[]
    {
        "I appreciate your confidence. I'll do my best to live up to it.",
        "Thank you for trusting me with this. I'm here to help.",
        "Your trust means a lot. Let's work on this together.",
        "I value your faith in me. I'll give you my best guidance."
    };

    private static readonly string[] AnticipationResponses = new[]
    {
        "I can feel your anticipation! This sounds exciting.",
        "Your enthusiasm for what's ahead is contagious!",
        "I share your excitement about what's coming!",
        "The future looks bright from where you're standing!"
    };

    private static readonly string[] DisgustResponses = new[]
    {
        "I understand that's unpleasant. Let's move past it.",
        "That does sound off-putting. I get it.",
        "I can sense your distaste. Fair enough.",
        "I hear you. Some things just don't sit right."
    };

    #endregion
}

/// <summary>
/// Comprehensive emotion analysis result
/// </summary>
public class EmotionAnalysis
{
    public string PrimaryEmotion { get; set; } = "neutral";
    public float Intensity { get; set; }
    public Dictionary<string, float> SecondaryEmotions { get; set; } = new();
    public string Sentiment { get; set; } = "neutral";
    public float SentimentScore { get; set; }
}
