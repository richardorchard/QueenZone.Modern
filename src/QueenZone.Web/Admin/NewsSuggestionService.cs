using System.Net;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web;

public abstract record SubmitOutcome
{
    private SubmitOutcome()
    {
    }

    public abstract string Message { get; }

    public sealed record Accepted(NewsSuggestion Suggestion) : SubmitOutcome
    {
        public override string Message => string.Empty;
    }

    public sealed record InvalidField(string message) : SubmitOutcome
    {
        public override string Message { get; } = message;
    }

    public sealed record DuplicateActive(string message) : SubmitOutcome
    {
        public override string Message { get; } = message;
    }

    public sealed record DailyLimit(string message) : SubmitOutcome
    {
        public override string Message { get; } = message;
    }

    public sealed record SignInRequired() : SubmitOutcome
    {
        public override string Message => "Sign in is required to suggest news.";
    }
}

public sealed class NewsSuggestionService(
    INewsSuggestionRepository newsSuggestionRepository,
    IOptions<NewsSuggestionOptions> options)
{
    public const string DuplicateActiveMessage =
        "This story has already been suggested — thank you, we are reviewing it.";

    public async Task<SubmitOutcome> SubmitAsync(
        Guid memberAccountId,
        string url,
        string? title,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (memberAccountId == Guid.Empty)
        {
            return new SubmitOutcome.SignInRequired();
        }

        var validationError = DescribeUrlProblem(url);
        if (validationError is not null)
        {
            return new SubmitOutcome.InvalidField(validationError);
        }

        if (!string.IsNullOrWhiteSpace(title) && title.Trim().Length > 300)
        {
            return new SubmitOutcome.InvalidField("Suggested headline must be 300 characters or fewer.");
        }

        if (!string.IsNullOrWhiteSpace(notes) && notes.Trim().Length > 1000)
        {
            return new SubmitOutcome.InvalidField("Notes must be 1000 characters or fewer.");
        }

        var normalizedUrl = NewsCandidateDedupe.NormalizeCanonicalUrl(url.Trim());
        var urlHash = NewsCandidateDedupe.ComputeUrlHash(normalizedUrl);

        if (await newsSuggestionRepository.HasActiveDuplicateAsync(urlHash, cancellationToken))
        {
            return new SubmitOutcome.DuplicateActive(DuplicateActiveMessage);
        }

        var maxPerDay = Math.Max(1, options.Value.MaxSubmissionsPerMemberPerDay);
        var sinceUtc = DateTimeOffset.UtcNow.AddDays(-1);
        var recentCount = await newsSuggestionRepository.CountBySubmitterSinceAsync(
            memberAccountId,
            sinceUtc,
            cancellationToken);
        if (recentCount >= maxPerDay)
        {
            return new SubmitOutcome.DailyLimit(
                $"You can suggest up to {maxPerDay} news stories per day. Please try again tomorrow.");
        }

        try
        {
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

            return new SubmitOutcome.Accepted(created);
        }
        catch (DuplicateActiveNewsSuggestionException)
        {
            return new SubmitOutcome.DuplicateActive(DuplicateActiveMessage);
        }
    }

    internal static string? ValidateUrl(string? url) => DescribeUrlProblem(url);

    internal static string? DescribeUrlProblem(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "URL is required.";
        }

        var trimmed = url.Trim();
        if (trimmed.Length > 2000)
        {
            return "URL must be 2000 characters or fewer.";
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return "URL must be a well-formed https:// link.";
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return "URL must not include credentials.";
        }

        if (IPAddress.TryParse(uri.DnsSafeHost, out var literal)
            && OutboundUrlSafety.IsBlockedAddress(literal))
        {
            return "URL must be a public https:// link.";
        }

        if (OutboundUrlSafety.IsBlockedHostName(uri.DnsSafeHost))
        {
            return "URL must be a public https:// link.";
        }

        return null;
    }
}
