namespace QueenZone.Web.Sitemap;

public sealed class SitemapIndexBuilder(ForumSitemapBuilder forumSitemapBuilder)
{
    public async Task<IReadOnlyList<SitemapIndexEntry>> BuildAsync(CancellationToken cancellationToken = default)
    {
        var lastModified = DateTime.UtcNow;
        var entries = new List<SitemapIndexEntry>
        {
            new("/sitemap-core.xml", lastModified)
        };

        var forumFileCount = await forumSitemapBuilder.GetFileCountAsync(cancellationToken);
        for (var fileNumber = 1; fileNumber <= forumFileCount; fileNumber++)
        {
            entries.Add(new($"/sitemap-forum-{fileNumber}.xml", lastModified));
        }

        return entries;
    }
}