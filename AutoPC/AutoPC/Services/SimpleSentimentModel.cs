using Microsoft.ML;

namespace AutoPC.Services;

public record SentimentResult(string Label, float Score);

/// <summary>
/// ML.NET sentiment model that supports retraining from user feedback.
/// Starts with base training data, then incrementally improves as feedback is collected.
/// </summary>
public class SimpleSentimentModel
{
    private readonly MLContext _mlContext;
    private PredictionEngine<SentimentData, SentimentPrediction> _predictor;
    private readonly object _retrainLock = new();

    // Base training data (always included in retraining to prevent catastrophic forgetting)
    private static readonly SentimentData[] BaseTrainingData = new[]
    {
        new SentimentData { Text = "I love this", Label = true },
        new SentimentData { Text = "This is fantastic", Label = true },
        new SentimentData { Text = "I am very happy", Label = true },
        new SentimentData { Text = "Great job, thank you", Label = true },
        new SentimentData { Text = "This is wonderful and helpful", Label = true },
        new SentimentData { Text = "Excellent work, very impressed", Label = true },
        new SentimentData { Text = "Amazing, exactly what I needed", Label = true },
        new SentimentData { Text = "Perfect answer, well done", Label = true },
        new SentimentData { Text = "I hate this", Label = false },
        new SentimentData { Text = "This is terrible", Label = false },
        new SentimentData { Text = "I am sad", Label = false },
        new SentimentData { Text = "Not helpful at all", Label = false },
        new SentimentData { Text = "This is wrong and confusing", Label = false },
        new SentimentData { Text = "Terrible response, useless", Label = false },
        new SentimentData { Text = "Very disappointed with this", Label = false },
        new SentimentData { Text = "Bad answer, completely wrong", Label = false },
    };

    // Tracking for retraining
    public DateTime? LastRetrainedAt { get; private set; }
    public double LastAccuracy { get; private set; }
    public int TrainingDataCount { get; private set; }
    public int RetrainCount { get; private set; }

    public SimpleSentimentModel()
    {
        _mlContext = new MLContext(seed: 0);
        TrainingDataCount = BaseTrainingData.Length;
        _predictor = BuildAndTrainModel(BaseTrainingData);
        Console.WriteLine($"[SentimentModel] Initialized with {TrainingDataCount} base samples");
    }

    public SentimentResult Predict(string text)
    {
        SentimentPrediction pred;
        lock (_retrainLock)
        {
            pred = _predictor.Predict(new SentimentData { Text = text });
        }
        var label = pred.PredictedLabel ? "Positive" : "Negative";
        return new SentimentResult(label, pred.Probability);
    }

    /// <summary>
    /// Retrain the model using feedback-derived training data combined with base data.
    /// Called by FeedbackService when enough new feedback has accumulated.
    /// </summary>
    public void RetrainWithFeedback(List<(string Text, bool IsPositive)> feedbackData)
    {
        if (feedbackData.Count == 0)
        {
            Console.WriteLine("[SentimentModel] No feedback data provided for retraining");
            return;
        }

        try
        {
            Console.WriteLine($"[SentimentModel] Retraining with {feedbackData.Count} feedback samples + {BaseTrainingData.Length} base samples");

            // Combine base data with feedback data
            var allData = new List<SentimentData>(BaseTrainingData);
            foreach (var (text, isPositive) in feedbackData)
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    allData.Add(new SentimentData { Text = text, Label = isPositive });
                }
            }

            // Deduplicate by text
            allData = allData
                .GroupBy(d => d.Text.ToLower().Trim())
                .Select(g => g.First())
                .ToList();

            var newPredictor = BuildAndTrainModel(allData.ToArray());

            // Evaluate on training data (simple accuracy check)
            var correct = 0;
            foreach (var sample in allData)
            {
                var pred = newPredictor.Predict(sample);
                if (pred.PredictedLabel == sample.Label)
                    correct++;
            }
            var accuracy = (double)correct / allData.Count;

            // Only accept the new model if accuracy is reasonable
            if (accuracy >= 0.5 || allData.Count < 20)
            {
                lock (_retrainLock)
                {
                    _predictor = newPredictor;
                }

                TrainingDataCount = allData.Count;
                LastRetrainedAt = DateTime.UtcNow;
                LastAccuracy = accuracy;
                RetrainCount++;

                Console.WriteLine($"[SentimentModel] Retrained successfully: {TrainingDataCount} samples, {accuracy:P1} accuracy, retrain #{RetrainCount}");
            }
            else
            {
                Console.WriteLine($"[SentimentModel] Retrained model rejected: {accuracy:P1} accuracy too low");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SentimentModel] Retraining error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get model diagnostics for the stats endpoint.
    /// </summary>
    public object GetDiagnostics()
    {
        return new
        {
            TrainingDataCount,
            LastRetrainedAt,
            LastAccuracy,
            RetrainCount,
            BaseDataCount = BaseTrainingData.Length
        };
    }

    private PredictionEngine<SentimentData, SentimentPrediction> BuildAndTrainModel(SentimentData[] data)
    {
        var trainingData = _mlContext.Data.LoadFromEnumerable(data);

        var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(SentimentData.Text))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(SentimentData.Label),
                featureColumnName: "Features"));

        var model = pipeline.Fit(trainingData);
        return _mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);
    }

    internal class SentimentData
    {
        public string Text { get; set; } = string.Empty;
        public bool Label { get; set; }
    }

    private class SentimentPrediction
    {
        public bool PredictedLabel { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }
}
