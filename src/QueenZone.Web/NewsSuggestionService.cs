using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web;

public sealed class NewsSuggestionService(
    INewsSuggestionRepository newsSuggestionRepository,
    IOptions<NewsSuggestionOptions> options)
{
    public const string DuplicateActiveMessage =
        "This story has already been suggested — thank you, we are reviewing it.";

    public sealed record SubmitResult(
        bool Succeeded,
        NewsSuggestion? Suggestion,
        string? Error,
        bool IsDuplicateActive);

    public async Task<SubmitResult> SubmitAsync(
        Guid memberAccountId,
        string url,
        string? title,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (memberAccountId == Guid.Empty)
        {
            return new SubmitResult(false, null, "Sign in is required to suggest news.", false);
        }

        var validationError = ValidateUrl(url);
        if (validationError is not null)
        {
            return new SubmitResult(false, null, validationError, false);
        }

        if (!string.IsNullOrWhiteSpace(title) && title.Trim().Length > 300)
        {
            return new SubmitResult(false, null, "Suggested headline must be 300 characters or fewer.", false);
        }

        if (!string.IsNullOrWhiteSpace(notes) && notes.Trim().Length > 1000)
        {
            return new SubmitResult(false, null, "Notes must be 1000 characters or fewer.", false);
        }

        var normalizedUrl = NewsCandidateDedupe.NormalizeCanonicalUrl(url.Trim());
        var urlHash = NewsCandidateDedupe.ComputeUrlHash(normalizedUrl);

        if (await newsSuggestionRepository.HasActiveDuplicateAsync(urlHash, cancellationToken))
        {
            return new SubmitResult(false, null, DuplicateActiveMessage, true);
        }

        var maxPerDay = Math.Max(1, options.Value.MaxSubmissionsPerMemberPerDay);
        var sinceUtc = DateTimeOffset.UtcNow.AddDays(-1);
        var recentCount = await newsSuggestionRepository.CountBySubmitterSinceAsync(
            memberAccountId,
            sinceUtc,
            cancellationToken);
        if (recentCount >= maxPerDay)
        {
            return new SubmitResult(
                false,
                null,
                $"You can suggest up to {maxPerDay} news stories per day. Please try again tomorrow.",
                false);
        }

        var created = await newsSuggestionRepository.CreateAsync(
            new NewsSuggestion(
                Guid.NewGuid(),
                memberAccountId,
                normalizedUrl,
                urlHash,
                string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
                string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                NewsSuggestionStatus.Pending,
                DateTimeOffset.UtcNow,
                null,
                null,
                null,
                null,
                null,
                null,
                null),
            cancellationToken);

        return new SubmitResult(true, created, null, false);
    }

    internal static string? ValidateUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "URL is required.";
        }

        if (url.Trim().Length > 2000)
        {
            return "URL must be 2000 characters or fewer.";
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            return "URL must be a well-formed https:// link.";
        }

        return null;
    }
}
