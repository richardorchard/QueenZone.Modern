using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Account;

[Authorize(Policy = MemberAuthenticationSchemes.MemberPolicy, AuthenticationSchemes = MemberAuthenticationSchemes.MembersCookie)]
public sealed class MySubmissionsModel(
    IPhotoSubmissionRepository photoSubmissionRepository,
    INewsSuggestionRepository newsSuggestionRepository,
    IArticleSubmissionRepository articleSubmissionRepository,
    INewsRepository newsRepository) : PageModel
{
    public const int PageSize = 10;

    public const string TabPhotos = "photos";
    public const string TabNews = "news";
    public const string TabArticles = "articles";

    public string ActiveTab { get; private set; } = TabPhotos;

    public int CurrentPage { get; private set; } = 1;

    public IReadOnlyList<PhotoSubmission> PhotoSubmissions { get; private set; } = [];

    public IReadOnlyList<NewsSuggestionRow> NewsSuggestions { get; private set; } = [];

    public IReadOnlyList<ArticleSubmission> ArticleSubmissions { get; private set; } = [];

    public ArchivePaginationViewModel? Pagination { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? tab, int? page, CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Redirect("/account/login");
        }

        ActiveTab = NormalizeTab(tab);
        CurrentPage = Math.Max(1, page ?? 1);

        switch (ActiveTab)
        {
            case TabNews:
            {
                var result = await newsSuggestionRepository.GetBySubmitterAsync(
                    memberId.Value, CurrentPage, PageSize, cancellationToken);
                NewsSuggestions = await MapNewsRowsAsync(result.Items, cancellationToken);
                Pagination = BuildPagination(result.TotalCount);
                break;
            }
            case TabArticles:
            {
                var result = await articleSubmissionRepository.GetDraftsForMemberAsync(
                    memberId.Value, CurrentPage, PageSize, cancellationToken);
                ArticleSubmissions = result.Items;
                Pagination = BuildPagination(result.TotalCount);
                break;
            }
            default:
            {
                var result = await photoSubmissionRepository.GetBySubmitterAsync(
                    memberId.Value, CurrentPage, PageSize, cancellationToken);
                PhotoSubmissions = result.Items;
                Pagination = BuildPagination(result.TotalCount);
                break;
            }
        }

        ViewData["Title"] = "My submissions";
        return Page();
    }

    public string GetTabHref(string tab) =>
        $"/account/my-submissions?tab={Uri.EscapeDataString(tab)}";

    public string GetArticleEditPath(Guid id) => $"/submit/article/{id:D}";

    private ArchivePaginationViewModel? BuildPagination(int totalCount)
    {
        var totalPages = ArchivePagination.GetTotalPages(totalCount, PageSize);
        return ArchivePagination.BuildViewModel(
            "My submissions pagination",
            CurrentPage,
            totalPages,
            pageNumber => pageNumber <= 1
                ? GetTabHref(ActiveTab)
                : $"{GetTabHref(ActiveTab)}&page={pageNumber}");
    }

    private async Task<IReadOnlyList<NewsSuggestionRow>> MapNewsRowsAsync(
        IReadOnlyList<NewsSuggestion> suggestions,
        CancellationToken cancellationToken)
    {
        var rows = new List<NewsSuggestionRow>(suggestions.Count);
        foreach (var suggestion in suggestions)
        {
            string? publishedPath = null;
            if (suggestion.Status == NewsSuggestionStatus.Promoted
                && suggestion.PromotedNewsId is int newsId)
            {
                var news = await newsRepository.GetByIdAsync(newsId, cancellationToken);
                if (news is { IsPublished: true })
                {
                    publishedPath = NewsRoutes.GetNewsDetailPath(news);
                }
            }

            rows.Add(new NewsSuggestionRow(suggestion, publishedPath));
        }

        return rows;
    }

    private static string NormalizeTab(string? tab) =>
        tab?.Trim().ToLowerInvariant() switch
        {
            TabNews => TabNews,
            TabArticles => TabArticles,
            _ => TabPhotos,
        };

    private async Task<Guid?> GetCurrentMemberIdAsync()
    {
        var authResult = await HttpContext.AuthenticateAsync(MemberAuthenticationSchemes.MembersCookie);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return null;
        }

        var idValue = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var id) ? id : null;
    }

    public sealed record NewsSuggestionRow(NewsSuggestion Suggestion, string? PublishedArticlePath);
}
