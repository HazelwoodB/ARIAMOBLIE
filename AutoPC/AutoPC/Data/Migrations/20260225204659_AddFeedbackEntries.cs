using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPC.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeedbackEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    IsHelpful = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UserQuery = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssistantResponse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedSentiment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SentimentScore = table.Column<float>(type: "real", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackEntries_CreatedAt_Desc",
                table: "FeedbackEntries",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackEntries_MessageId",
                table: "FeedbackEntries",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackEntries_Rating",
                table: "FeedbackEntries",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackEntries_UserId",
                table: "FeedbackEntries",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedbackEntries");
        }
    }
}
