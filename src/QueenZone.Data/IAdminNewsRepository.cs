namespace QueenZone.Data;

public interface IAdminNewsRepository
{
    Task<IReadOnlyList<AdminNewsArticle>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AdminNewsArticlePage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<AdminNewsArticle?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateDraftAsync(AdminNewsDraft draft, string editorEmail, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, AdminNewsDraft draft, string editorEmail, CancellationToken cancellationToken = default);

    Task PublishAsync(int id, string editorEmail, CancellationToken cancellationToken = default);

    Task UnpublishAsync(int id, string editorEmail, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, string editorEmail, CancellationToken cancellationToken = default);

    Task<bool> IsSlugInUseAsync(string slug, int? excludeNewsId = null, CancellationToken cancellationToken = default);
}