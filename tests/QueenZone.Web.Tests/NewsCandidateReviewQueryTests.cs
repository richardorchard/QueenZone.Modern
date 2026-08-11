using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class NewsCandidateReviewQueryTests
{
    [Fact]
    public void ApplyEntityFilters_in_memory_filters_status_source_confidence_and_dates()
    {
        var source = new NewsDiscoverySourceEntity
        {
            Id = 1,
            DisplayName = "Primary feed",
            TrustTier = NewsDiscoveryTrustTier.Primary,
        };
        var candidates = new[]
        {
            new NewsCandidateEntity
            {
                Id = 1,
                SourceId = 1,
                Status = NewsCandidateStatus.NeedsReview,
                ConfidenceScore = 0.9m,
                DiscoveredAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new NewsCandidateEntity
            {
                Id = 2,
                SourceId = 1,
                Status = NewsCandidateStatus.Discovered,
                ConfidenceScore = 0.2m,
                DiscoveredAt = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            },
        };

        var filtered = NewsCandidateReviewQuery.ApplyEntityFilters(
            candidates,
            new NewsCandidateListQuery(
                Status: NewsCandidateStatus.NeedsReview,
                MinConfidence: 0.5m,
                DiscoveredFromUtc: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                DiscoveredToUtc: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)),
            _ => source).ToList();

        Assert.Single(filtered);
        Assert.Equal(1, filtered[0].Id);
    }

    [Fact]
    public void ApplyActiveQueueFilter_excludes_rejected_promoted_and_duplicate_ignored_when_status_unset()
    {
        var candidates = new[]
        {
            new NewsCandidateEntity { Id = 1, Status = NewsCandidateStatus.NeedsReview },
            new NewsCandidateEntity { Id = 2, Status = NewsCandidateStatus.Rejected },
            new NewsCandidateEntity { Id = 3, Status = NewsCandidateStatus.PromotedToArticle },
            new NewsCandidateEntity { Id = 4, Status = NewsCandidateStatus.IgnoredDuplicate },
        };

        var filtered = NewsCandidateReviewQuery.ApplyActiveQueueFilter(
            candidates,
            new NewsCandidateListQuery()).ToList();

        Assert.Single(filtered);
        Assert.Equal(1, filtered[0].Id);
    }
}
