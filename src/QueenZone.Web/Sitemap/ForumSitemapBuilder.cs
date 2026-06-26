using QueenZone.Data;

namespace QueenZone.Web.Sitemap;

public sealed class ForumSitemapBuilder(IForumRepository forumRepository)
{
    public async Task<int> GetFileCountAsync(CancellationToken cancellationToken = default)
    {
        var topicCount = await forumRepository.GetTopicSitemapCountAsync(cancellationToken);
        if (topicCount <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(topicCount / (double)ForumSitemapLimits.MaxUrlsPerFile);
    }

    public async Task<IReadOnlyList<SitemapEntry>?> BuildFileAsync(
        int fileNumber,
        CancellationToken cancellationToken = default)
    {
        if (fileNumber < 1)
        {
            return null;
        }

        var fileCount = await GetFileCountAsync(cancellationToken);
        if (fileNumber > fileCount)
        {
            return null;
        }

        var offset = (fileNumber - 1) * ForumSitemapLimits.MaxUrlsPerFile;
        var topics = await forumRepository.GetTopicSitemapPageAsync(
            offset,
            ForumSitemapLimits.MaxUrlsPerFile,
            cancellationToken);

        return topics
            .Select(topic => new SitemapEntry(
                ForumRoutes.GetTopicCanonicalPath(topic.TopicId, topic.Title),
                topic.LastActivityAt))
            .ToList();
    }
}