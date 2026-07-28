using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.FreddieTributes;

public sealed class IndexModel(IAdminFreddieTributeRepository tributeRepository) : AdminFreddieTributesPageModel
{
    public const int PageSize = 40;

    [BindProperty(SupportsGet = true)]
    public string? Visibility { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool DuplicatesOnly { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public AdminFreddieTributePage? Tributes { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Tributes = await tributeRepository.GetPageAsync(BuildFilter(), PageNumber, PageSize, cancellationToken);
        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        ViewData["Title"] = "Freddie tributes";
    }

    public string BuildPageQuery(int pageNumber)
    {
        var parts = new List<string> { $"pageNumber={pageNumber}" };
        if (!string.IsNullOrWhiteSpace(Visibility))
        {
            parts.Add($"visibility={Uri.EscapeDataString(Visibility)}");
        }

        if (!string.IsNullOrWhiteSpace(Q))
        {
            parts.Add($"q={Uri.EscapeDataString(Q)}");
        }

        if (DuplicatesOnly)
        {
            parts.Add("duplicatesOnly=true");
        }

        return string.Join("&", parts);
    }

    private AdminFreddieTributeListFilter BuildFilter()
    {
        bool? isVisible = Visibility?.ToLowerInvariant() switch
        {
            "visible" => true,
            "hidden" => false,
            _ => null,
        };

        return new AdminFreddieTributeListFilter(isVisible, Q, DuplicatesOnly);
    }
}

