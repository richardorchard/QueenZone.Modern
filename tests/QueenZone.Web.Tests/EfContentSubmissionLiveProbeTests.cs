using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

/// <summary>
/// Opt-in SQL Express mirror probe for photo and article submission write lifecycles.
/// </summary>
[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfContentSubmissionLiveProbeTests
{
    [Fact]
    public async Task Photo_submission_create_and_review_status_when_enabled()
    {
        if (!IsProbeEnabled(out var connectionString))
        {
            return;
        }

        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var memberId = Guid.NewGuid();
        var marker = $"photo-submission-probe-{uniqueSuffix}";
        Guid? submissionId = null;

        try
        {
            await using var context = CreateContext(connectionString);
            context.MemberAccounts.Add(NewProbeMember(
                memberId,
                $"{marker}@queenzone.local",
                $"Photo Submission Probe {uniqueSuffix}"));
            await context.SaveChangesAsync();

            var repo = new EfPhotoSubmissionRepository(context);
            var created = await repo.CreateAsync(new NewPhotoSubmission(
                memberId,
                $"{marker} title",
                "Disposable mirror photo submission probe.",
                "Live",
                1986,
                null,
                $"probe/{marker}/original.jpg",
                $"probe/{marker}/web.jpg",
                $"probe/{marker}/thumb.jpg",
                "probe.jpg",
                1024,
                "image/jpeg",
                100,
                80));
            submissionId = created.Id;
            Assert.Equal(PhotoSubmissionStatus.Pending, created.Status);

            var underReview = await repo.UpdateStatusAsync(
                created.Id,
                PhotoSubmissionStatus.UnderReview,
                "photo-submission-probe@queenzone.local",
                "Mirror probe under review.",
                null);
            Assert.NotNull(underReview);
            Assert.Equal(PhotoSubmissionStatus.UnderReview, underReview.Status);

            var rejected = await repo.UpdateStatusAsync(
                created.Id,
                PhotoSubmissionStatus.Rejected,
                "photo-submission-probe@queenzone.local",
                "Mirror probe rejection.",
                "Rejected by disposable mirror probe.");
            Assert.NotNull(rejected);
            Assert.Equal(PhotoSubmissionStatus.Rejected, rejected.Status);
        }
        finally
        {
            await CleanupPhotoAsync(connectionString, memberId, submissionId, marker);
        }
    }

    [Fact]
    public async Task Article_submission_draft_submit_and_reject_when_enabled()
    {
        if (!IsProbeEnabled(out var connectionString))
        {
            return;
        }

        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var memberId = Guid.NewGuid();
        var marker = $"article-submission-probe-{uniqueSuffix}";
        Guid? submissionId = null;
        var body = new string('x', EfArticleSubmissionRepository.MinBodyVisibleChars + 20);

        try
        {
            await using var context = CreateContext(connectionString);
            context.MemberAccounts.Add(NewProbeMember(
                memberId,
                $"{marker}@queenzone.local",
                $"Article Submission Probe {uniqueSuffix}"));
            await context.SaveChangesAsync();

            var repo = new EfArticleSubmissionRepository(context);
            var draft = await repo.UpsertDraftAsync(new ArticleSubmissionDraft(
                null,
                memberId,
                $"{marker} title",
                "Disposable mirror article submission excerpt.",
                body,
                null,
                "probe"));
            submissionId = draft.Id;
            Assert.Equal(ArticleSubmissionStatus.Draft, draft.Status);

            var submitted = await repo.SubmitForReviewAsync(draft.Id, memberId);
            Assert.NotNull(submitted);
            Assert.Equal(ArticleSubmissionStatus.Submitted, submitted.Status);

            var rejected = await repo.UpdateStatusAsync(
                draft.Id,
                ArticleSubmissionStatus.Rejected,
                "article-submission-probe@queenzone.local",
                "Mirror probe rejection notes.",
                "Rejected by disposable mirror probe.");
            Assert.NotNull(rejected);
            Assert.Equal(ArticleSubmissionStatus.Rejected, rejected.Status);
        }
        finally
        {
            await CleanupArticleAsync(connectionString, memberId, submissionId, marker);
        }
    }

    private static QueenZoneDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.CommandTimeout(QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: QueenZoneSqlServerOptions.MaxRetryCount,
                        maxRetryDelay: QueenZoneSqlServerOptions.MaxRetryDelay,
                        errorNumbersToAdd: null);
                })
            .Options;
        return new QueenZoneDbContext(options);
    }

    private static MemberAccount NewProbeMember(Guid id, string email, string displayName) =>
        new()
        {
            Id = id,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        };

    private static async Task CleanupPhotoAsync(
        string connectionString,
        Guid memberId,
        Guid? submissionId,
        string marker)
    {
        await using var cleanup = CreateContext(connectionString);
        if (submissionId is Guid id)
        {
            await cleanup.PhotoSubmissionAuditLogs
                .Where(a => a.PhotoSubmissionId == id)
                .ExecuteDeleteAsync();
            await cleanup.PhotoSubmissions
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync();
        }

        await cleanup.MemberAccounts
            .Where(m => m.Id == memberId || m.Email.Contains(marker))
            .ExecuteDeleteAsync();
    }

    private static async Task CleanupArticleAsync(
        string connectionString,
        Guid memberId,
        Guid? submissionId,
        string marker)
    {
        await using var cleanup = CreateContext(connectionString);
        if (submissionId is Guid id)
        {
            await cleanup.ArticleSubmissions
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync();
        }

        await cleanup.MemberAccounts
            .Where(m => m.Id == memberId || m.Email.Contains(marker))
            .ExecuteDeleteAsync();
    }

    private static bool IsProbeEnabled(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_CONTENT_SUBMISSION_PROBE"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
