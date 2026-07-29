namespace QueenZone.Data;

public sealed class InMemoryAdminPhotoRepository(SharedPhotoStore store) : IAdminPhotoRepository
{
    public Task<AdminPhotoPage> GetPageAsync(
        AdminPhotoListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var all = store.GetPhotos(filter);
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, 200);
        var items = all
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return Task.FromResult(new AdminPhotoPage(items, all.Count, safePage, safePageSize));
    }

    public Task<AdminPhotoItem?> GetByIdAsync(int picId, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetPhoto(picId));

    public Task<IReadOnlyList<AdminPhotoCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetCategories());

    public Task<AdminPhotoCategory?> GetCategoryByIdAsync(int catId, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetCategory(catId));

    public Task<int> CreateAsync(
        AdminPhotoCreateRequest request,
        string editorEmail,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Create(request, editorEmail));

    public Task UpdateAsync(
        int picId,
        AdminPhotoUpdateRequest request,
        string editorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!store.Update(picId, request, editorEmail))
        {
            throw new InvalidOperationException($"Photo {picId} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task SetVisibilityAsync(
        int picId,
        bool isVisible,
        string editorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!store.SetVisibility(picId, isVisible, editorEmail))
        {
            throw new InvalidOperationException($"Photo {picId} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAssetsAsync(
        int picId,
        AdminPhotoAssetUpdate assets,
        string editorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!store.UpdateAssets(picId, assets, editorEmail))
        {
            throw new InvalidOperationException($"Photo {picId} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateThumbnailAsync(
        int picId,
        string legacyThumbUrl,
        int thumbWidth,
        int thumbHeight,
        string editorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!store.UpdateThumbnail(picId, legacyThumbUrl, thumbWidth, thumbHeight, editorEmail))
        {
            throw new InvalidOperationException($"Photo {picId} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(int picId, string editorEmail, CancellationToken cancellationToken = default)
    {
        if (!store.Delete(picId, editorEmail))
        {
            throw new InvalidOperationException($"Photo {picId} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task AppendAuditAsync(
        int picId,
        string action,
        string actorEmail,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        store.AppendAudit(picId, action, actorEmail, details);
        return Task.CompletedTask;
    }
}
