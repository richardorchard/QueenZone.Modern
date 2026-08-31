namespace QueenZone.Data;

public interface IAdminFreddieTributeRepository
{
    Task<AdminFreddieTributePage> GetPageAsync(
        AdminFreddieTributeListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminFreddieTributeItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task SetVisibilityAsync(
        int id,
        bool isVisible,
        string editorEmail,
        bool? expectedIsVisible = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        string editorEmail,
        bool? expectedIsVisible = null,
        CancellationToken cancellationToken = default);
}

