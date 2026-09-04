using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using QueenZone.Data;
using QueenZone.Search.Shared;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web;

/// <summary>
/// Admin create / update / hide for published fan performances, plus the public-cache
/// and search-index side effects that keep <c>/fan-performances</c> and <c>/api/v1</c>
/// current without a process restart. Hide never deletes the <c>songfiles</c> blob.
/// </summary>
public sealed class AdminFanPerformanceWriteService(
    IAdminFanPerformanceRepository adminFanPerformanceRepository,
    PublicQueryCacheService publicQueryCache,
    CoreSitemapService coreSitemapService,
    IOutputCacheStore outputCacheStore,
    ISearchIndexService searchIndexService,
    ILogger<AdminFanPerformanceWriteService> logger)
{
    public async Task<int> CreateAsync(
        AdminFanPerformanceCreateRequest request,
        string editorEmail,
        CancellationToken cancellationToken = default)
    {
        var id = await adminFanPerformanceRepository.CreateAsync(request, editorEmail, cancellationToken);
        var created = await adminFanPerformanceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Fan performance {id} was not found after create.");
        await AfterWriteAsync(created, cancellationToken);
        return id;
    }

    public async Task UpdateAsync(
        int id,
        AdminFanPerformanceUpdateRequest request,
        string editorEmail,
        AdminFanPerformanceConcurrencyToken? expected = null,
        CancellationToken cancellationToken = default)
    {
        await adminFanPerformanceRepository.UpdateAsync(id, request, editorEmail, expected, cancellationToken);
        var updated = await adminFanPerformanceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Fan performance {id} was not found.");
        await AfterWriteAsync(updated, cancellationToken);
    }

    public async Task SetVisibilityAsync(
        int id,
        bool isVisible,
        string editorEmail,
        bool? expectedIsVisible = null,
        CancellationToken cancellationToken = default)
    {
        await adminFanPerformanceRepository.SetVisibilityAsync(
            id,
            isVisible,
            editorEmail,
            expectedIsVisible,
            cancellationToken);
        var updated = await adminFanPerformanceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Fan performance {id} was not found.");
        await AfterWriteAsync(updated, cancellationToken);
    }

    public Task HideAsync(
        int id,
        string editorEmail,
        bool? expectedIsVisible = null,
        CancellationToken cancellationToken = default) =>
        SetVisibilityAsync(id, isVisible: false, editorEmail, expectedIsVisible, cancellationToken);

    private async Task AfterWriteAsync(AdminFanPerformanceItem item, CancellationToken cancellationToken)
    {
        await InvalidatePublicCachesAsync(cancellationToken);
        await SyncSearchIndexAsync(item, cancellationToken);
    }

    internal async Task InvalidatePublicCachesAsync(CancellationToken cancellationToken)
    {
        publicQueryCache.InvalidateFanPerformanceCache();
        await coreSitemapService.InvalidateAsync(cancellationToken);
        await outputCacheStore.EvictByTagAsync(PublicOutputCachePolicies.PublicHtmlTag, cancellationToken);
    }

    private async Task SyncSearchIndexAsync(AdminFanPerformanceItem item, CancellationToken cancellationToken)
    {
        try
        {
            if (item.IsVisible)
            {
                await searchIndexService.UpsertAsync(
                    SearchReindexBuilder.MapFanPerformance(item.ToFanPerformance()),
                    cancellationToken);
                return;
            }

            await searchIndexService.RemoveAsync(
                SearchReindexBuilder.FanPerformanceSourceKey(item.Id),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Best-effort search index sync failed for fan performance {FanPerformanceId}",
                item.Id);
        }
    }
}
