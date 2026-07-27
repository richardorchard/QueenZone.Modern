using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.FreddieTribute;

public sealed class TributePageModel(
    IFreddieTributeRepository tributeRepository,
    IPhotoRepository photoRepository) : FreddieTributeArchivePageModel(tributeRepository, photoRepository)
{
    public async Task<IActionResult> OnGetAsync(int pageNumber, CancellationToken cancellationToken) =>
        await LoadArchivePageAsync(pageNumber, cancellationToken);
}
