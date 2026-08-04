using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// One indexed, publicly visible unit of site content. Only visible/published records are ever
/// written here — there is no visibility-status column, so a stale row is the only way a
/// previously-visible item could leak through search after it stops being visible. Keeping the
/// corresponding <see cref="ISearchIndexService"/> call next to every publish/withdraw/moderate
/// mutation (plus a periodic full reindex as a correctness backstop) is what keeps this true.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SearchDocumentEntity
{
    public Guid Id { get; set; }

    /// <summary>Stable source identity, e.g. <c>news:123</c>, <c>forum-thread:4521</c>, <c>article:some-slug</c>.</summary>
    public string SourceKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>Plain text only — stripped of HTML at index time. Full-text indexed.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Short plain-text excerpt shown on result cards, separate from <see cref="Body"/>.</summary>
    public string? Summary { get; set; }

    public string Url { get; set; } = string.Empty;

    public DateTimeOffset? PublishedAt { get; set; }

    public string? ImageUrl { get; set; }

    public string? Category { get; set; }

    public string? AuthorDisplayName { get; set; }

    public DateTimeOffset IndexedAt { get; set; }
}
