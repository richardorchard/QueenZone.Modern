using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Pages.Admin.Photos;

public sealed class IndexModel(IAdminPhotoRepository adminPhotoRepository) : AdminPhotosPageModel
{
    public const int PageSize = 40;

    [BindProperty(SupportsGet = true)]
    public int? CatId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Visibility { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public AdminPhotoPage? Photos { get; private set; }

    public IReadOnlyList<AdminPhotoCategory> Categories { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Categories = await adminPhotoRepository.GetCategoriesAsync(cancellationToken);
        Photos = await adminPhotoRepository.GetPageAsync(BuildFilter(), PageNumber, PageSize, cancellationToken);
        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        ViewData["Title"] = "Photos";
    }

    private AdminPhotoListFilter BuildFilter()
    {
        bool? isVisible = Visibility?.ToLowerInvariant() switch
        {
            "visible" => true,
            "hidden" => false,
            _ => null,
        };

        return new AdminPhotoListFilter(CatId, isVisible, Year, Q);
    }
}

public sealed class NewModel(IAdminPhotoRepository adminPhotoRepository) : AdminPhotosPageModel
{
    public IReadOnlyList<AdminPhotoCategory> Categories { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Categories = await adminPhotoRepository.GetCategoriesAsync(cancellationToken);
        ViewData["Title"] = "Add photo";
    }
}

public sealed class CreateModel(
    AdminPhotoService adminPhotoService,
    PublicQueryCacheService publicQueryCache,
    CoreSitemapService coreSitemapService,
    IOutputCacheStore outputCacheStore) : AdminPhotosPageModel
{
    public async Task<IActionResult> OnPostAsync(
        IFormFile? file,
        int catId,
        string title,
        string? keywords,
        int year,
        DateTime? dateTime,
        bool isVisible = false,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            TempData[MessageKey] = "A photo file is required.";
            TempData[MessageKindKey] = "error";
            return Redirect("/admin/photos/new");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData[MessageKey] = "Title is required.";
            TempData[MessageKindKey] = "error";
            return Redirect("/admin/photos/new");
        }

        try
        {
            var picId = await adminPhotoService.CreateAsync(
                file,
                catId,
                title,
                keywords,
                year > 0 ? year : DateTime.UtcNow.Year,
                dateTime ?? DateTime.UtcNow,
                isVisible,
                EditorEmail,
                cancellationToken);

            if (isVisible)
            {
                await ActionModel.InvalidatePublicPhotoCachesAsync(
                    publicQueryCache,
                    coreSitemapService,
                    outputCacheStore,
                    cancellationToken);
            }

            TempData[MessageKey] = $"Created photo #{picId}.";
            TempData[MessageKindKey] = "success";
            return Redirect($"/admin/photos/{picId}");
        }
        catch (InvalidOperationException ex)
        {
            TempData[MessageKey] = ex.Message;
            TempData[MessageKindKey] = "error";
            return Redirect("/admin/photos/new");
        }
    }
}

public sealed class EditModel(IAdminPhotoRepository adminPhotoRepository) : AdminPhotosPageModel
{
    public AdminPhotoItem? Photo { get; private set; }

    public IReadOnlyList<AdminPhotoCategory> Categories { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Photo = await adminPhotoRepository.GetByIdAsync(id, cancellationToken);
        if (Photo is null)
        {
            return NotFound();
        }

        Categories = await adminPhotoRepository.GetCategoriesAsync(cancellationToken);
        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        ViewData["Title"] = $"Edit photo — {Photo.Title}";
        return Page();
    }
}

public sealed class EditPostModel(
    IAdminPhotoRepository adminPhotoRepository,
    AdminPhotoService adminPhotoService,
    PublicQueryCacheService publicQueryCache,
    CoreSitemapService coreSitemapService,
    IOutputCacheStore outputCacheStore) : AdminPhotosPageModel
{
    public async Task<IActionResult> OnPostAsync(
        int id,
        string title,
        string? keywords,
        int year,
        DateTime dateTime,
        int catId,
        IFormFile? replaceFile,
        CancellationToken cancellationToken = default)
    {
        var existing = await adminPhotoRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData[MessageKey] = "Title is required.";
            TempData[MessageKindKey] = "error";
            return Redirect($"/admin/photos/{id}");
        }

        try
        {
            await adminPhotoRepository.UpdateAsync(
                id,
                new AdminPhotoUpdateRequest(title, keywords, year, dateTime, catId),
                EditorEmail,
                cancellationToken);

            if (replaceFile is { Length: > 0 })
            {
                await adminPhotoService.ReplaceAsync(id, replaceFile, EditorEmail, cancellationToken);
            }

            await ActionModel.InvalidatePublicPhotoCachesAsync(
                publicQueryCache,
                coreSitemapService,
                outputCacheStore,
                cancellationToken);
            TempData[MessageKey] = "Photo updated.";
            TempData[MessageKindKey] = "success";
            return Redirect($"/admin/photos/{id}");
        }
        catch (InvalidOperationException ex)
        {
            TempData[MessageKey] = ex.Message;
            TempData[MessageKindKey] = "error";
            return Redirect($"/admin/photos/{id}");
        }
    }
}

public sealed class ActionModel(
    IAdminPhotoRepository adminPhotoRepository,
    AdminPhotoService adminPhotoService,
    PublicQueryCacheService publicQueryCache,
    CoreSitemapService coreSitemapService,
    IOutputCacheStore outputCacheStore) : AdminPhotosPageModel
{
    public IActionResult OnGet(int id) => Redirect("/admin/photos");

    public async Task<IActionResult> OnPostHideAsync(int id, CancellationToken cancellationToken)
    {
        await adminPhotoRepository.SetVisibilityAsync(id, false, EditorEmail, cancellationToken);
        await InvalidatePublicPhotoCachesAsync(publicQueryCache, coreSitemapService, outputCacheStore, cancellationToken);
        TempData[MessageKey] = "Photo hidden from public gallery.";
        TempData[MessageKindKey] = "success";
        return Redirect($"/admin/photos/{id}");
    }

    public async Task<IActionResult> OnPostShowAsync(int id, CancellationToken cancellationToken)
    {
        await adminPhotoRepository.SetVisibilityAsync(id, true, EditorEmail, cancellationToken);
        await InvalidatePublicPhotoCachesAsync(publicQueryCache, coreSitemapService, outputCacheStore, cancellationToken);
        TempData[MessageKey] = "Photo is now visible.";
        TempData[MessageKindKey] = "success";
        return Redirect($"/admin/photos/{id}");
    }

    public async Task<IActionResult> OnPostRegenerateThumbAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await adminPhotoService.RegenerateThumbnailAsync(id, EditorEmail, cancellationToken);
            await InvalidatePublicPhotoCachesAsync(publicQueryCache, coreSitemapService, outputCacheStore, cancellationToken);
            TempData[MessageKey] = "Thumbnail regenerated.";
            TempData[MessageKindKey] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData[MessageKey] = ex.Message;
            TempData[MessageKindKey] = "error";
        }

        return Redirect($"/admin/photos/{id}");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        await adminPhotoRepository.DeleteAsync(id, EditorEmail, cancellationToken);
        await InvalidatePublicPhotoCachesAsync(publicQueryCache, coreSitemapService, outputCacheStore, cancellationToken);
        TempData[MessageKey] = $"Deleted photo #{id} (database row only; blobs left in place).";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/photos");
    }

    internal static async Task InvalidatePublicPhotoCachesAsync(
        PublicQueryCacheService publicQueryCache,
        CoreSitemapService coreSitemapService,
        IOutputCacheStore outputCacheStore,
        CancellationToken cancellationToken)
    {
        publicQueryCache.InvalidatePhotoCache();
        await coreSitemapService.InvalidateAsync(cancellationToken);
        await outputCacheStore.EvictByTagAsync(PublicOutputCachePolicies.PublicHtmlTag, cancellationToken);
    }
}
