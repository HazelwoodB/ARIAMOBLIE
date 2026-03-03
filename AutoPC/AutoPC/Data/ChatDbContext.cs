using Microsoft.EntityFrameworkCore;
using AutoPC.Models;

namespace AutoPC.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<FeedbackEntry> FeedbackEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ChatMessage entity
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Message)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.Sentiment)
                .HasMaxLength(50);

            entity.Property(e => e.Score);

            // Create index for faster queries
            entity.HasIndex(e => e.Timestamp)
                .IsDescending()
                .HasDatabaseName("IX_ChatMessages_Timestamp_Desc");

            entity.HasIndex(e => e.Role)
                .HasDatabaseName("IX_ChatMessages_Role");
        });

        // Configure FeedbackEntry entity
        modelBuilder.Entity<FeedbackEntry>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.MessageId)
                .IsRequired();

            entity.Property(e => e.Rating)
                .IsRequired();

            entity.Property(e => e.Comment)
                .HasColumnType("nvarchar(1000)");

            entity.Property(e => e.IsHelpful)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.UserQuery)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.AssistantResponse)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.DetectedSentiment)
                .HasMaxLength(50);

            entity.Property(e => e.SentimentScore);

            // Store tags as comma-separated string
            entity.Property(e => e.TagsSerialized)
                .HasColumnName("Tags")
                .HasMaxLength(500);

            // Ignore the string[] Tags property - use TagsSerialized for DB
            entity.Ignore(e => e.Tags);

            // Indexes
            entity.HasIndex(e => e.MessageId)
                .HasDatabaseName("IX_FeedbackEntries_MessageId");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_FeedbackEntries_UserId");

            entity.HasIndex(e => e.Rating)
                .HasDatabaseName("IX_FeedbackEntries_Rating");

            entity.HasIndex(e => e.CreatedAt)
                .IsDescending()
                .HasDatabaseName("IX_FeedbackEntries_CreatedAt_Desc");
        });
    }
}
