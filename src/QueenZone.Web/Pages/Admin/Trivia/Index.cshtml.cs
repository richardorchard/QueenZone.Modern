using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Pages.Admin.Trivia;

public sealed class IndexModel(
    ITriviaRepository triviaRepository,
    IOutputCacheStore outputCacheStore) : AdminTriviaPageModel
{
    public IReadOnlyList<TriviaFactItem> Facts { get; private set; } = [];

    public IReadOnlyList<string> Categories { get; private set; } = [];

    public string? CategoryFilter { get; private set; }

    public TriviaFormViewModel? CreateForm { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task OnGetAsync(string? category, CancellationToken cancellationToken)
    {
        CategoryFilter = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        var all = await triviaRepository.GetAllAsync(cancellationToken);
        Categories = all
            .Select(fact => fact.Category)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Facts = CategoryFilter is null
            ? all
            : all.Where(fact =>
                    fact.Category is not null &&
                    fact.Category.Contains(CategoryFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        ViewData["Title"] = "Trivia";
    }

    public async Task<IActionResult> OnPostAsync(
        [FromForm] AdminTriviaForm form,
        CancellationToken cancellationToken)
    {
        var draft = form.ToDraft();
        var errors = TriviaValidation.ValidateDraft(draft);
        if (errors.Count > 0)
        {
            ViewData["Title"] = "Add trivia fact";
            CreateForm = NewModel.BuildForm(draft, errors);
            return Page();
        }

        await triviaRepository.CreateAsync(draft, cancellationToken);
        await InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);
        TempData[MessageKey] = "Added trivia fact.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/trivia");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        await triviaRepository.DeleteAsync(id, cancellationToken);
        await InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);
        TempData[MessageKey] = "Deleted trivia fact.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/trivia");
    }

    public async Task<IActionResult> OnPostTogglePublishAsync(
        int id,
        bool isPublished,
        CancellationToken cancellationToken)
    {
        await triviaRepository.SetPublishedAsync(id, !isPublished, cancellationToken);
        await InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);
        TempData[MessageKey] = !isPublished ? "Trivia fact published." : "Trivia fact unpublished.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/trivia");
    }

    internal static async Task InvalidatePublicHomeCacheAsync(
        IOutputCacheStore outputCacheStore,
        CancellationToken cancellationToken)
    {
        await outputCacheStore.EvictByTagAsync(PublicOutputCachePolicies.PublicHtmlTag, cancellationToken);
    }
}
