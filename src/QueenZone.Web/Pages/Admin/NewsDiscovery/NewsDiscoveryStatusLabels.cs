using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.NewsDiscovery;

public static class NewsDiscoveryStatusLabels
{
    public static string Format(NewsCandidateStatus status) => status switch
    {
        NewsCandidateStatus.Discovered => "Discovered",
        NewsCandidateStatus.NeedsReview => "Needs review",
        NewsCandidateStatus.Drafted => "Drafted",
        NewsCandidateStatus.Rejected => "Rejected",
        NewsCandidateStatus.IgnoredDuplicate => "Ignored duplicate",
        NewsCandidateStatus.PromotedToArticle => "Promoted",
        _ => status.ToString()
    };

    public static string FormatTrustTier(NewsDiscoveryTrustTier trustTier) => trustTier switch
    {
        NewsDiscoveryTrustTier.Primary => "Primary",
        NewsDiscoveryTrustTier.Secondary => "Secondary",
        _ => trustTier.ToString()
    };
}
