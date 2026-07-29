namespace QueenZone.Data;

public sealed class SharedNewsStore
{
    private readonly object sync = new();
    private readonly List<AdminNewsArticle> articles = [];
    private readonly List<NewsAuditEntry> auditEntries = [];
    private int nextAuditId = 1;
    private int nextNewsId = 10_000;

    public SharedNewsStore()
    {
    }

    public SharedNewsStore(IEnumerable<AdminNewsArticle> seedArticles)
    {
        lock (sync)
        {
            articles.AddRange(seedArticles);
            nextNewsId = articles.Count == 0 ? 10_000 : articles.Max(article => article.Id) + 1;
        }
    }

    public IReadOnlyList<AdminNewsArticle> GetAllArticles()
    {
        lock (sync)
        {
            return articles
                .OrderByDescending(article => article.PublishedAt)
                .ThenByDescending(article => article.Id)
                .ToList();
        }
    }

    public AdminNewsArticle? GetArticle(int id)
    {
        lock (sync)
        {
            return articles.SingleOrDefault(article => article.Id == id);
        }
    }

    public IReadOnlyList<NewsItem> GetPublishedNewsItems()
    {
        lock (sync)
        {
            return articles
                .Where(article => article.IsPublished)
                .OrderByDescending(article => article.PublishedAt)
                .ThenByDescending(article => article.Id)
                .Select(ToNewsItem)
                .ToList();
        }
    }

    public int CreateDraft(AdminNewsDraft draft, string editorEmail)
    {
        lock (sync)
        {
            var id = nextNewsId++;
            var timestamp = DateTime.UtcNow;
            articles.Add(new AdminNewsArticle(
                id,
                draft.Title,
                NewsSlug.Resolve(draft.Title, draft.Slug),
                draft.Excerpt,
                draft.Body,
                draft.PublishedAt,
                draft.SourceUrl,
                false,
                timestamp,
                timestamp,
                editorEmail));
            return id;
        }
    }

    public bool Update(int id, AdminNewsDraft draft, string editorEmail)
    {
        lock (sync)
        {
            var index = articles.FindIndex(article => article.Id == id);
            if (index < 0)
            {
                return false;
            }

            var existing = articles[index];
            articles[index] = existing with
            {
                Title = draft.Title,
                Slug = NewsSlug.Resolve(draft.Title, draft.Slug),
                Excerpt = draft.Excerpt,
                Body = draft.Body,
                PublishedAt = draft.PublishedAt,
                SourceUrl = draft.SourceUrl,
                UpdatedAt = DateTime.UtcNow,
                EditorEmail = editorEmail
            };
            return true;
        }
    }

    public bool SetPublished(int id, bool isPublished, string editorEmail)
    {
        lock (sync)
        {
            var index = articles.FindIndex(article => article.Id == id);
            if (index < 0)
            {
                return false;
            }

            var existing = articles[index];
            articles[index] = existing with
            {
                IsPublished = isPublished,
                UpdatedAt = DateTime.UtcNow,
                EditorEmail = editorEmail
            };
            return true;
        }
    }

    public bool Delete(int id)
    {
        lock (sync)
        {
            var index = articles.FindIndex(article => article.Id == id);
            if (index < 0)
            {
                return false;
            }

            articles.RemoveAt(index);
            return true;
        }
    }

    public bool IsSlugInUse(string slug, int? excludeNewsId)
    {
        lock (sync)
        {
            var normalized = NewsSlug.Slugify(slug);
            return articles.Any(article =>
                article.Id != excludeNewsId
                && string.Equals(NewsSlug.ResolveForArticle(article), normalized, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void AppendAudit(int newsId, string action, string actorEmail, string? details)
    {
        lock (sync)
        {
            auditEntries.Add(new NewsAuditEntry(
                nextAuditId++,
                newsId,
                action,
                actorEmail,
                DateTime.UtcNow,
                details));
        }
    }

    public IReadOnlyList<NewsAuditEntry> GetAuditEntries(int newsId)
    {
        lock (sync)
        {
            return auditEntries
                .Where(entry => entry.NewsId == newsId)
                .OrderByDescending(entry => entry.OccurredAt)
                .ThenByDescending(entry => entry.Id)
                .ToList();
        }
    }

    private static NewsItem ToNewsItem(AdminNewsArticle article) =>
        new(
            article.Id,
            article.Title,
            article.Excerpt,
            article.Body,
            article.PublishedAt,
            article.SourceUrl,
            article.IsPublished,
            string.IsNullOrWhiteSpace(article.Slug) ? null : article.Slug);
}