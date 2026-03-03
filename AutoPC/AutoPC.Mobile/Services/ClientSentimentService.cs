namespace AutoPC.Client.Services;

/// <summary>
/// Client-side sentiment analysis using a simple keyword-based approach.
/// This is lightweight and runs entirely in the browser.
/// For production, integrate with ONNX Runtime or TensorFlow.js for ML models.
/// </summary>
public class ClientSentimentService
{
    private static readonly Dictionary<string, float> PositiveWords = new()
    {
        { "good", 0.8f }, { "great", 0.9f }, { "excellent", 0.95f }, { "amazing", 0.9f },
        { "wonderful", 0.9f }, { "perfect", 0.95f }, { "love", 0.85f }, { "fantastic", 0.9f },
        { "awesome", 0.85f }, { "best", 0.9f }, { "happy", 0.85f }, { "glad", 0.8f },
        { "pleased", 0.8f }, { "delighted", 0.85f }, { "brilliant", 0.9f }, { "outstanding", 0.9f },
        { "superb", 0.9f }, { "beautiful", 0.85f }, { "nice", 0.7f }, { "cool", 0.7f },
        { "thanks", 0.7f }, { "thank", 0.7f }, { "grateful", 0.8f }
    };

    private static readonly Dictionary<string, float> NegativeWords = new()
    {
        { "bad", -0.8f }, { "terrible", -0.95f }, { "awful", -0.9f }, { "horrible", -0.95f },
        { "hate", -0.9f }, { "worst", -0.95f }, { "disgusting", -0.9f }, { "poor", -0.8f },
        { "sad", -0.8f }, { "angry", -0.85f }, { "frustrated", -0.8f }, { "annoyed", -0.75f },
        { "disappointed", -0.85f }, { "upset", -0.8f }, { "wrong", -0.7f }, { "broken", -0.8f },
        { "useless", -0.85f }, { "stupid", -0.9f }, { "dumb", -0.85f }, { "pathetic", -0.9f },
        { "fail", -0.85f }, { "failed", -0.85f }, { "error", -0.7f }, { "problem", -0.65f }
    };

    /// <summary>
    /// Analyzes sentiment of text using keyword matching.
    /// Returns positive/negative label and confidence score.
    /// </summary>
    public SentimentResult AnalyzeSentiment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new SentimentResult { Label = "Neutral", Score = 0, IsPositive = false };

        var lowerText = text.ToLower();
        var words = lowerText.Split(new[] { ' ', '\t', '\n', '\r', ',', '.', '!', '?' }, 
            StringSplitOptions.RemoveEmptyEntries);

        float totalScore = 0;
        int sentimentWordCount = 0;

        foreach (var word in words)
        {
            if (PositiveWords.TryGetValue(word, out var positiveScore))
            {
                totalScore += positiveScore;
                sentimentWordCount++;
            }
            else if (NegativeWords.TryGetValue(word, out var negativeScore))
            {
                totalScore += negativeScore;
                sentimentWordCount++;
            }
        }

        // Calculate average sentiment
        float sentiment = sentimentWordCount > 0 ? totalScore / sentimentWordCount : 0;

        // Determine label and whether it's positive
        string label = sentiment > 0.1f ? "Positive" : sentiment < -0.1f ? "Negative" : "Neutral";
        bool isPositive = sentiment > 0.1f;

        return new SentimentResult
        {
            Label = label,
            Score = Math.Abs(sentiment),
            IsPositive = isPositive
        };
    }
}

/// <summary>
/// Result from sentiment analysis.
/// </summary>
public class SentimentResult
{
    public string Label { get; set; } = "Neutral";
    public float Score { get; set; }
    public bool IsPositive { get; set; }
}
