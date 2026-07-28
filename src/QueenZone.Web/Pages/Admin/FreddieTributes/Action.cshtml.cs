using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.FreddieTributes;

public sealed class ActionModel(
    IAdminFreddieTributeRepository tributeRepository,
    IOutputCacheStore outputCacheStore) : AdminFreddieTributesPageModel
{
    public IActionResult OnGet(int id) => Redirect("/admin/freddie-tributes");

    public async Task<IActionResult> OnPostHideAsync(int id, CancellationToken cancellationToken)
    {
        await tributeRepository.SetVisibilityAsync(id, false, EditorEmail, cancellationToken);
        await InvalidatePublicHtmlAsync(outputCacheStore, cancellationToken);
        TempData[MessageKey] = $"Freddie tribute #{id} hidden from the public archive.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/freddie-tributes");
    }

    public async Task<IActionResult> OnPostShowAsync(int id, CancellationToken cancellationToken)
    {
        await tributeRepository.SetVisibilityAsync(id, true, EditorEmail, cancellationToken);
        await InvalidatePublicHtmlAsync(outputCacheStore, cancellationToken);
        TempData[MessageKey] = $"Freddie tribute #{id} restored to the public archive.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/freddie-tributes?visibility=hidden");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        await tributeRepository.DeleteAsync(id, EditorEmail, cancellationToken);
        await InvalidatePublicHtmlAsync(outputCacheStore, cancellationToken);
        TempData[MessageKey] = $"Freddie tribute #{id} deleted.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/freddie-tributes");
    }

    private static async Task InvalidatePublicHtmlAsync(
        IOutputCacheStore outputCacheStore,
        CancellationToken cancellationToken) =>
        await outputCacheStore.EvictByTagAsync(PublicOutputCachePolicies.PublicHtmlTag, cancellationToken);
}

