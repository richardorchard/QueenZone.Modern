using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

/// <summary>
/// Opt-in SQL Express mirror probe for photo and article submission write lifecycles,
/// including photo-submission promotion into legacy <c>PIC_FILES_T</c>/<c>PIC_CAT_T</c>.
/// </summary>
[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfContentSubmissionLiveProbeTests
{
    private const string ProbeEditor = "photo-submission-probe@queenzone.local";

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
                ProbeEditor,
                "Mirror probe under review.",
                null);
            Assert.NotNull(underReview);
            Assert.Equal(PhotoSubmissionStatus.UnderReview, underReview.Status);

            var rejected = await repo.UpdateStatusAsync(
                created.Id,
                PhotoSubmissionStatus.Rejected,
                ProbeEditor,
                "Mirror probe rejection.",
                "Rejected by disposable mirror probe.");
            Assert.NotNull(rejected);
            Assert.Equal(PhotoSubmissionStatus.Rejected, rejected.Status);
        }
        finally
        {
            await CleanupPhotoAsync(connectionString, memberId, submissionId, marker, promotedPicId: null);
        }
    }

    /// <summary>
    /// Self-seeds a pending photo submission, promotes it through the same repository
    /// path as <c>PhotoSubmissionPromotionService</c> (legacy <c>PIC_FILES_T</c> insert +
    /// submission <c>PromoteAsync</c>), asserts a visible gallery row joins
    /// <c>PIC_CAT_T</c>, then deletes all probe rows. Blob copy is skipped — that is
    /// covered by unit tests; this probe targets real SQL Server column types.
    /// </summary>
    [Fact]
    public async Task Photo_submission_promotion_writes_visible_pic_files_row_when_enabled()
    {
        if (!IsProbeEnabled(out var connectionString))
        {
            return;
        }

        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var memberId = Guid.NewGuid();
        var marker = $"photo-submission-probe-{uniqueSuffix}";
        var title = $"{marker} promotion title";
        Guid? submissionId = null;
        int? promotedPicId = null;

        try
        {
            await using var context = CreateContext(connectionString);
            context.MemberAccounts.Add(NewProbeMember(
                memberId,
                $"{marker}@queenzone.local",
                $"Photo Submission Promotion Probe {uniqueSuffix}"));
            await context.SaveChangesAsync();

            var submissionRepo = new EfPhotoSubmissionRepository(context);
            var adminPhotoRepo = new EfAdminPhotoRepository(context);

            var categories = await adminPhotoRepo.GetCategoriesAsync();
            Assert.NotEmpty(categories);
            var category = categories[0];
            Assert.True(category.CatId > 0);
            Assert.False(string.IsNullOrWhiteSpace(category.Name));

            var created = await submissionRepo.CreateAsync(new NewPhotoSubmission(
                memberId,
                title,
                "Disposable mirror photo submission promotion probe.",
                category.Name,
                1986,
                new DateOnly(1986, 7, 12),
                $"probe/{marker}/original.jpg",
                $"probe/{marker}/web.webp",
                $"probe/{marker}/thumb.webp",
                "probe.webp",
                2048,
                "image/webp",
                800,
                600));
            submissionId = created.Id;
            Assert.Equal(PhotoSubmissionStatus.Pending, created.Status);
            Assert.Null(created.PromotedPicId);

            // Mirrors PhotoSubmissionPromotionService.PromoteCoreAsync without blob I/O.
            var stem = Guid.NewGuid().ToString("N");
            var originalFileName = stem + ".webp";
            var thumbFileName = stem + "_t.webp";
            var legacyUrl = PhotoLegacyPath.BuildLegacyPath(category.Name, originalFileName);
            var legacyThumbUrl = PhotoLegacyPath.BuildLegacyPath(category.Name, thumbFileName);
            const int thumbSize = 400;

            promotedPicId = await adminPhotoRepo.CreateAsync(
                new AdminPhotoCreateRequest(
                    category.CatId,
                    created.Title,
                    Keywords: null,
                    created.ApproximateYear ?? DateTime.UtcNow.Year,
                    created.ApproximateDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow,
                    IsVisible: true,
                    legacyUrl,
                    legacyThumbUrl,
                    thumbSize,
                    thumbSize,
                    created.ImageWidthPx ?? 0,
                    created.ImageHeightPx ?? 0),
                ProbeEditor);

            var promoted = await submissionRepo.PromoteAsync(
                created.Id,
                promotedPicId.Value,
                category.Name,
                ProbeEditor,
                "Mirror promotion probe notes.");
            Assert.NotNull(promoted);
            Assert.Equal(PhotoSubmissionStatus.Approved, promoted.Status);
            Assert.Equal(promotedPicId.Value, promoted.PromotedPicId);
            Assert.Equal(category.Name, promoted.ApprovedCategory);

            var publicRow = await adminPhotoRepo.GetByIdAsync(promotedPicId.Value);
            Assert.NotNull(publicRow);
            Assert.Equal(promotedPicId.Value, publicRow.PicId);
            Assert.Equal(category.CatId, publicRow.CatId);
            Assert.Equal(category.Name, publicRow.CategoryName);
            Assert.Equal(title, publicRow.Title);
            Assert.True(publicRow.IsVisible);
            Assert.Equal(legacyUrl, publicRow.LegacyUrl);
            Assert.Equal(legacyThumbUrl, publicRow.LegacyThumbUrl);
            Assert.Equal(1986, publicRow.Year);
            Assert.Equal(800, publicRow.PictureWidth);
            Assert.Equal(600, publicRow.PictureHeight);

            // Direct join proof that PIC_FILES_T + PIC_CAT_T materialize on real SQL Server.
            var joinCount = await context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT CAST(COUNT(*) AS int) AS [Value]
                    FROM dbo.PIC_FILES_T p
                    INNER JOIN dbo.PIC_CAT_T c ON c.cat_id = p.Cat_ID
                    WHERE p.PIC_ID = {0}
                      AND p.DISPLAY = 1
                      AND c.cat_id = {1}
                    """,
                    promotedPicId.Value,
                    category.CatId)
                .SingleAsync();
            Assert.Equal(1, joinCount);
        }
        finally
        {
            await CleanupPhotoAsync(connectionString, memberId, submissionId, marker, promotedPicId);
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
        string marker,
        int? promotedPicId)
    {
        await using var cleanup = CreateContext(connectionString);

        if (promotedPicId is int picId)
        {
            // Prefer raw cleanup over DeleteAsync so we do not leave an extra audit row
            // if the PIC row is already gone.
            await cleanup.Database.ExecuteSqlRawAsync(
                """
                DELETE FROM dbo.PIC_FILES_T
                WHERE PIC_ID = {0}
                   OR Name LIKE {1}
                """,
                picId,
                marker + "%");

            await cleanup.PhotoAdminAuditLogs
                .Where(a => a.PicId == picId || a.ActorEmail == ProbeEditor)
                .ExecuteDeleteAsync();
        }
        else
        {
            // Status-only probe never touches PIC_FILES_T; still scrub probe-named leftovers.
            await cleanup.Database.ExecuteSqlRawAsync(
                """
                DELETE FROM dbo.PIC_FILES_T
                WHERE Name LIKE {0}
                """,
                marker + "%");

            await cleanup.PhotoAdminAuditLogs
                .Where(a => a.ActorEmail == ProbeEditor)
                .ExecuteDeleteAsync();
        }

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
