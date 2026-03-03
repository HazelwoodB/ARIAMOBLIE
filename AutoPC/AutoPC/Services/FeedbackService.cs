using AutoPC.Data;
using AutoPC.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPC.Services;

/// <summary>
/// Server-side feedback service - handles persistence, statistics, and ML retraining coordination.
/// Stores all user feedback in SQL database for durable cross-device access and ML training.
/// </summary>
public class FeedbackService
{
    private readonly ChatDbContext _dbContext;
    private readonly SimpleSentimentModel _sentimentModel;

    public FeedbackService(ChatDbContext dbContext, SimpleSentimentModel sentimentModel)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _sentimentModel = sentimentModel ?? throw new ArgumentNullException(nameof(sentimentModel));
    }

    /// <summary>
    /// Submit feedback from a client. Persists to database and triggers retraining check.
    /// </summary>
    public async Task<FeedbackEntry> SubmitFeedbackAsync(FeedbackSubmitRequest request, CancellationToken ct = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var entry = new FeedbackEntry
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            MessageId = request.MessageId,
            Rating = request.Rating,
            Comment = request.Comment,
            Tags = request.Tags ?? Array.Empty<string>(),
            IsHelpful = request.Rating >= 4,
            CreatedAt = DateTime.UtcNow,
            UserQuery = request.UserQuery,
            AssistantResponse = request.AssistantResponse,
            DetectedSentiment = request.DetectedSentiment,
            SentimentScore = request.SentimentScore
        };

        _dbContext.FeedbackEntries.Add(entry);
        await _dbContext.SaveChangesAsync(ct);

        Console.WriteLine($"[FeedbackService] Saved feedback: {entry.Rating}★ for message {entry.MessageId}");

        // Check if we should trigger ML retraining
        await CheckAndRetrainAsync(ct);

        return entry;
    }

    /// <summary>
    /// Get aggregated feedback statistics.
    /// </summary>
    public async Task<FeedbackStatsResponse> GetStatsAsync(CancellationToken ct = default)
    {
        var entries = await _dbContext.FeedbackEntries.ToListAsync(ct);

        var stats = new FeedbackStatsResponse
        {
            TotalFeedback = entries.Count,
            AverageRating = entries.Count > 0 ? entries.Average(e => e.Rating) : 0,
            HelpfulCount = entries.Count(e => e.IsHelpful),
            UnhelpfulCount = entries.Count(e => !e.IsHelpful),
            RatingBreakdown = new Dictionary<int, int>
            {
                [1] = entries.Count(e => e.Rating == 1),
                [2] = entries.Count(e => e.Rating == 2),
                [3] = entries.Count(e => e.Rating == 3),
                [4] = entries.Count(e => e.Rating == 4),
                [5] = entries.Count(e => e.Rating == 5)
            },
            TotalTrainingSamples = _sentimentModel.TrainingDataCount,
            LastRetrainedAt = _sentimentModel.LastRetrainedAt,
            ModelAccuracy = _sentimentModel.LastAccuracy,
            LatestFeedbackAt = entries.OrderByDescending(e => e.CreatedAt).FirstOrDefault()?.CreatedAt
        };

        return stats;
    }

    /// <summary>
    /// Get feedback for a specific message.
    /// </summary>
    public async Task<FeedbackEntry?> GetFeedbackForMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        return await _dbContext.FeedbackEntries
            .FirstOrDefaultAsync(e => e.MessageId == messageId, ct);
    }

    /// <summary>
    /// Get all feedback for a user.
    /// </summary>
    public async Task<List<FeedbackEntry>> GetFeedbackForUserAsync(Guid userId, int limit = 100, CancellationToken ct = default)
    {
        return await _dbContext.FeedbackEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Get all feedback (for export/analysis).
    /// </summary>
    public async Task<List<FeedbackEntry>> GetAllFeedbackAsync(int limit = 500, CancellationToken ct = default)
    {
        return await _dbContext.FeedbackEntries
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Export feedback as training data for ML models.
    /// Returns (text, isPositive) pairs derived from rated interactions.
    /// </summary>
    public async Task<List<(string Text, bool IsPositive)>> ExportTrainingDataAsync(CancellationToken ct = default)
    {
        var entries = await _dbContext.FeedbackEntries
            .Where(e => !string.IsNullOrEmpty(e.UserQuery))
            .OrderByDescending(e => e.CreatedAt)
            .Take(1000)
            .ToListAsync(ct);

        var trainingData = new List<(string Text, bool IsPositive)>();

        foreach (var entry in entries)
        {
            // Use the user's query as training text, with sentiment derived from rating
            // High ratings on responses to positive queries reinforce positive classification
            // Low ratings on responses suggest the interaction context was negative
            bool isPositive = entry.Rating >= 4;

            if (!string.IsNullOrWhiteSpace(entry.UserQuery))
            {
                trainingData.Add((entry.UserQuery, isPositive));
            }

            // If there's a comment, it provides additional training signal
            if (!string.IsNullOrWhiteSpace(entry.Comment))
            {
                // A comment on a low rating is negative sentiment; on a high rating is positive
                trainingData.Add((entry.Comment, isPositive));
            }
        }

        return trainingData;
    }

    /// <summary>
    /// Check if we have enough new feedback to trigger ML retraining,
    /// and retrain the model if so.
    /// </summary>
    private async Task CheckAndRetrainAsync(CancellationToken ct)
    {
        try
        {
            var totalFeedback = await _dbContext.FeedbackEntries.CountAsync(ct);
            var lastRetrained = _sentimentModel.LastRetrainedAt;

            // Retrain if: 10+ feedback entries AND (never retrained OR 10+ new entries since last retrain)
            bool shouldRetrain = false;

            if (totalFeedback >= 10 && lastRetrained == null)
            {
                shouldRetrain = true;
            }
            else if (lastRetrained.HasValue)
            {
                var newSinceRetrain = await _dbContext.FeedbackEntries
                    .CountAsync(e => e.CreatedAt > lastRetrained.Value, ct);
                if (newSinceRetrain >= 10)
                {
                    shouldRetrain = true;
                }
            }

            if (shouldRetrain)
            {
                Console.WriteLine("[FeedbackService] Triggering ML model retraining...");
                var trainingData = await ExportTrainingDataAsync(ct);
                if (trainingData.Count >= 10)
                {
                    _sentimentModel.RetrainWithFeedback(trainingData);
                    Console.WriteLine($"[FeedbackService] Model retrained with {trainingData.Count} samples");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FeedbackService] Retrain check error: {ex.Message}");
        }
    }

    /// <summary>
    /// Force a model retrain with all available feedback data.
    /// </summary>
    public async Task ForceRetrainAsync(CancellationToken ct = default)
    {
        var trainingData = await ExportTrainingDataAsync(ct);
        if (trainingData.Count > 0)
        {
            _sentimentModel.RetrainWithFeedback(trainingData);
            Console.WriteLine($"[FeedbackService] Force retrained model with {trainingData.Count} samples");
        }
        else
        {
            Console.WriteLine("[FeedbackService] No feedback data available for retraining");
        }
    }
}
