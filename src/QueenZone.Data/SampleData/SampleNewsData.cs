namespace QueenZone.Data;

public static class SampleNewsData
{
    public static IReadOnlyList<AdminNewsArticle> CreateSeedArticles()
    {
        var timestamp = new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc);
        var articles = new List<AdminNewsArticle>
        {
            new(
                1003,
                "QueenZone modernisation begins",
                "queenzone-modernisation-begins",
                "The first local vertical slice is now running from the new ASP.NET Core application.",
                "This placeholder item keeps the local site useful until a restored legacy SQL Server connection is configured.",
                timestamp,
                null,
                true,
                timestamp,
                timestamp,
                null),
            new(
                1002,
                "Legacy news archive mapped",
                "legacy-news-archive-mapped",
                "The first migration target is NEWS_T, with clean canonical routes for the modern archive.",
                "News gives the rebuild a narrow, valuable slice through data access, routing, page rendering, search-friendly URLs, and deployment.",
                new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc),
                null,
                true,
                timestamp,
                timestamp,
                null),
            new(
                1001,
                "Read-only launch strategy accepted",
                "read-only-launch-strategy-accepted",
                "The first public release will focus on preserving archive content before restoring interactive features.",
                "Accounts, posting, uploads, private messages, and administration are intentionally out of scope for the first release.",
                new DateTime(2026, 6, 9, 9, 0, 0, DateTimeKind.Utc),
                null,
                true,
                timestamp,
                timestamp,
                null),
            new(
                9001,
                "Hidden moderation draft",
                "hidden-moderation-draft",
                "This record should never appear in public archive output.",
                "Moderated content remains in the legacy database but is excluded from the modern read-only archive.",
                new DateTime(2026, 6, 12, 9, 0, 0, DateTimeKind.Utc),
                null,
                false,
                timestamp,
                timestamp,
                null)
        };

        for (var id = 1004; id <= 1022; id++)
        {
            var dayOffset = 1022 - id;
            articles.Add(new AdminNewsArticle(
                id,
                $"Archive sample article {id}",
                $"archive-sample-article-{id}",
                $"Excerpt for archive sample article {id}.",
                $"Body for archive sample article {id}.",
                new DateTime(2026, 6, 8, 9, 0, 0, DateTimeKind.Utc).AddDays(-dayOffset),
                null,
                true,
                timestamp,
                timestamp,
                null));
        }

        return articles;
    }
}