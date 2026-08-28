using System.Security.Claims;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Stores article card images in <see cref="BlobUploadContainers.Articles"/> and
/// quietly deletes replaced ugc-articles blobs. Gallery / PIC references are never deleted.
/// </summary>
public sealed class NewsArticleImageService(
    IBlobUploadService blobUploadService,
    MemberUploadQuotaService uploadQuota)
{
    public sealed record ApplyResult(AdminNewsDraft Draft, string? Error);

    public async Task<ApplyResult> TryApplyAsync(
        IFormFile? file,
        NewsArticleImageCrop? crop,
        AdminNewsDraft draft,
        string? previousImageBlobKey,
        ClaimsPrincipal user,
        bool persist,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0)
        {
            return new ApplyResult(draft, null);
        }

        NewsArticleImageProcessor.ProcessedArticleImage processed;
        try
        {
            await using var source = file.OpenReadStream();
            processed = await NewsArticleImageProcessor.ProcessAsync(
                source,
                file.FileName,
                crop,
                NewsArticleImageProcessor.MaxUploadBytes,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return new ApplyResult(draft, ex.Message);
        }

        await using (processed.FullImage)
        await using (processed.Thumbnail)
        {
            if (!persist)
            {
                return new ApplyResult(draft, null);
            }

            var principalKey = MemberUploadQuotaService.PrincipalKeyFromUser(user);
            var quotaBytes = Math.Max(processed.FullImage.Length, 1);
            if (!uploadQuota.TryConsume(principalKey, quotaBytes, out var quotaError))
            {
                return new ApplyResult(draft, quotaError ?? "Daily upload limit reached.");
            }

            var context = BuildUploadContext(user);
            string? uploadedFull = null;
            string? uploadedThumb = null;
            try
            {
                var fullName = BuildBlobName(context);

                processed.FullImage.Position = 0;
                var fullResult = await blobUploadService.UploadAsync(
                    processed.FullImage,
                    "article.webp",
                    BlobUploadContainers.Articles,
                    CloneContext(context, fullName),
                    cancellationToken);
                uploadedFull = fullResult.BlobName;
                uploadedThumb = UgcProxyPaths.ToThumbBlobName(uploadedFull);

                processed.Thumbnail.Position = 0;
                await blobUploadService.UploadAsync(
                    processed.Thumbnail,
                    "article-thumb.webp",
                    BlobUploadContainers.Articles,
                    CloneContext(context, uploadedThumb),
                    cancellationToken);

                var nextDraft = draft with
                {
                    ImageBlobKey = uploadedFull,
                    ImageGalleryPicId = null,
                };

                await TryDeletePreviousUgcArticlesAsync(previousImageBlobKey, uploadedFull, cancellationToken);
                return new ApplyResult(nextDraft, null);
            }
            catch (NotSupportedException ex)
            {
                await TryDeleteQuietlyAsync(uploadedFull, cancellationToken);
                await TryDeleteQuietlyAsync(uploadedThumb, cancellationToken);
                return new ApplyResult(draft, ex.Message);
            }
            catch (BlobUploadException ex)
            {
                await TryDeleteQuietlyAsync(uploadedFull, cancellationToken);
                await TryDeleteQuietlyAsync(uploadedThumb, cancellationToken);
                return new ApplyResult(draft, ex.Message);
            }
            catch (Exception)
            {
                await TryDeleteQuietlyAsync(uploadedFull, cancellationToken);
                await TryDeleteQuietlyAsync(uploadedThumb, cancellationToken);
                throw;
            }
        }
    }

    internal static async Task TryDeletePreviousUgcArticlesAsync(
        IBlobUploadService blobs,
        string? previousImageBlobKey,
        string? replacementBlobName,
        CancellationToken cancellationToken)
    {
        var previous = NewsArticleImage.ArticlesBlobName(previousImageBlobKey);
        if (string.IsNullOrWhiteSpace(previous))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(replacementBlobName)
            && string.Equals(previous, replacementBlobName, StringComparison.Ordinal))
        {
            return;
        }

        await TryDeleteQuietlyAsync(blobs, previous, cancellationToken);
        await TryDeleteQuietlyAsync(blobs, UgcProxyPaths.ToThumbBlobName(previous), cancellationToken);
    }

    private Task TryDeletePreviousUgcArticlesAsync(
        string? previousImageBlobKey,
        string? replacementBlobName,
        CancellationToken cancellationToken) =>
        TryDeletePreviousUgcArticlesAsync(
            blobUploadService,
            previousImageBlobKey,
            replacementBlobName,
            cancellationToken);

    private Task TryDeleteQuietlyAsync(string? blobName, CancellationToken cancellationToken) =>
        TryDeleteQuietlyAsync(blobUploadService, blobName, cancellationToken);

    private static async Task TryDeleteQuietlyAsync(
        IBlobUploadService blobs,
        string? blobName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return;
        }

        try
        {
            await blobs.DeleteAsync(BlobUploadContainers.Articles, blobName, cancellationToken);
        }
        catch
        {
            // Cleanup is best-effort, matching editor-image and avatar quiet-delete.
        }
    }

    private static BlobUploadContext BuildUploadContext(ClaimsPrincipal user)
    {
        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("preferred_username")
            ?? user.Identity?.Name;

        Guid? memberAccountId = null;
        var memberIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(memberIdValue, out var parsed) && parsed != Guid.Empty)
        {
            memberAccountId = parsed;
        }

        return new BlobUploadContext
        {
            ActorEmail = email,
            MemberAccountId = memberAccountId,
        };
    }

    private static string BuildBlobName(BlobUploadContext context)
    {
        var generated = BlobNameGenerator.Create("article.webp", context);
        if (!generated.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            generated += ".webp";
        }

        return generated;
    }

    private static BlobUploadContext CloneContext(BlobUploadContext source, string preferredBlobName) =>
        new()
        {
            MemberAccountId = source.MemberAccountId,
            MemberId = source.MemberId,
            ActorEmail = source.ActorEmail,
            PreferredBlobName = preferredBlobName,
        };
}
