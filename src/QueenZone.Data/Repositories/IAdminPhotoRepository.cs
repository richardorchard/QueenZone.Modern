namespace QueenZone.Data;

public interface IAdminPhotoRepository
{
    Task<AdminPhotoPage> GetPageAsync(
        AdminPhotoListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminPhotoItem?> GetByIdAsync(int picId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminPhotoCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<AdminPhotoCategory?> GetCategoryByIdAsync(int catId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(AdminPhotoCreateRequest request, string editorEmail, CancellationToken cancellationToken = default);

    Task UpdateAsync(int picId, AdminPhotoUpdateRequest request, string editorEmail, CancellationToken cancellationToken = default);

    Task SetVisibilityAsync(int picId, bool isVisible, string editorEmail, CancellationToken cancellationToken = default);

    Task UpdateAssetsAsync(int picId, AdminPhotoAssetUpdate assets, string editorEmail, CancellationToken cancellationToken = default);

    Task UpdateThumbnailAsync(
        int picId,
        string legacyThumbUrl,
        int thumbWidth,
        int thumbHeight,
        string editorEmail,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int picId, string editorEmail, CancellationToken cancellationToken = default);

    Task AppendAuditAsync(
        int picId,
        string action,
        string actorEmail,
        string? details = null,
        CancellationToken cancellationToken = default);
}
