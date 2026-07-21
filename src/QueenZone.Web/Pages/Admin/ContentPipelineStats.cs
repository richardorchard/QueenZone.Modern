using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin;

public sealed record RecentlyPublishedDashboardItem(
    string Title,
    DateTime PublishedAt,
    string Kind,
    string Href);

public sealed record ContentPipelineStats(
    int PendingCandidates,
    NewsAiPipelineHealth AiPipeline,
    IReadOnlyList<RecentlyPublishedDashboardItem> RecentlyPublished)
{
    public static readonly ContentPipelineStats Empty =
        new(0, NewsAiPipelineHealth.Empty, []);
}
