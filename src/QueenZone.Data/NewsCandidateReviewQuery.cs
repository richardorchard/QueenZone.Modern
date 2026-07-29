using QueenZone.Data.Entities;

namespace QueenZone.Data;

/// <summary>
/// Shared review-list filter application for discovery candidates.
/// Used by both <see cref="EfNewsDiscoveryRepository"/> and <see cref="SharedNewsDiscoveryStore"/>
/// so status/source/confidence/date filters stay aligned (#399).
/// </summary>
/// <remarks>
/// HasDraft and Entity filters stay in each implementation because they depend on different
/// draft/AI-run storage shapes (EF subqueries vs in-memory collections).
/// </remarks>
public static class NewsCandidateReviewQuery
{
    /// <summary>
    /// Applies filters that map 1:1 onto <see cref="NewsCandidateEntity"/> columns / source trust tier.
    /// Caller must ensure <c>Source</c> is available when <see cref="NewsCandidateListQuery.TrustTier"/> is set.
    /// </summary>
    public static IQueryable<NewsCandidateEntity> ApplyEntityFilters(
        IQueryable<NewsCandidateEntity> candidates,
        NewsCandidateListQuery query)
    {
        if (query.Status is not null)
        {
            candidates = candidates.Where(candidate => candidate.Status == query.Status);
        }

        if (query.SourceId is not null)
        {
            candidates = candidates.Where(candidate => candidate.SourceId == query.SourceId);
        }

        if (query.TrustTier is not null)
        {
            candidates = candidates.Where(candidate => candidate.Source!.TrustTier == query.TrustTier);
        }

        if (query.MinConfidence is not null)
        {
            candidates = candidates.Where(candidate =>
                candidate.ConfidenceScore != null
                && candidate.ConfidenceScore >= query.MinConfidence);
        }

        if (query.DiscoveredFromUtc is not null)
        {
            candidates = candidates.Where(candidate => candidate.DiscoveredAt >= query.DiscoveredFromUtc);
        }

        if (query.DiscoveredToUtc is not null)
        {
            candidates = candidates.Where(candidate => candidate.DiscoveredAt < query.DiscoveredToUtc);
        }

        return candidates;
    }

    /// <summary>
    /// In-memory equivalent of <see cref="ApplyEntityFilters"/> when Source is resolved via a lookup.
    /// </summary>
    public static IEnumerable<NewsCandidateEntity> ApplyEntityFilters(
        IEnumerable<NewsCandidateEntity> candidates,
        NewsCandidateListQuery query,
        Func<int, NewsDiscoverySourceEntity?> resolveSource)
    {
        IEnumerable<NewsCandidateEntity> queryable = candidates;

        if (query.Status is not null)
        {
            queryable = queryable.Where(candidate => candidate.Status == query.Status);
        }

        if (query.SourceId is not null)
        {
            queryable = queryable.Where(candidate => candidate.SourceId == query.SourceId);
        }

        if (query.TrustTier is not null)
        {
            queryable = queryable.Where(candidate =>
            {
                var source = resolveSource(candidate.SourceId);
                return source?.TrustTier == query.TrustTier;
            });
        }

        if (query.MinConfidence is not null)
        {
            queryable = queryable.Where(candidate =>
                candidate.ConfidenceScore is not null
                && candidate.ConfidenceScore >= query.MinConfidence);
        }

        if (query.DiscoveredFromUtc is not null)
        {
            queryable = queryable.Where(candidate => candidate.DiscoveredAt >= query.DiscoveredFromUtc);
        }

        if (query.DiscoveredToUtc is not null)
        {
            queryable = queryable.Where(candidate => candidate.DiscoveredAt < query.DiscoveredToUtc);
        }

        return queryable;
    }
}
