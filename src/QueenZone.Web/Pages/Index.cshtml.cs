using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages;

public sealed class IndexModel(INewsRepository newsRepository) : PageModel
{
    public IReadOnlyList<NewsItem> Latest { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "QueenZone";
        Latest = await newsRepository.GetLatestAsync(5, cancellationToken);
    }
}
