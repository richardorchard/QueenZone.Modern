using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class GalleryOriginalModel(
    IAdminPhotoRepository adminPhotoRepository,
    IGalleryPhotoBlobService galleryPhotoBlobService) : AdminNewsPageModel
{
    public async Task<IActionResult> OnGetAsync(int picId, CancellationToken cancellationToken)
    {
        var original = await NewsArticleGalleryPicker.OpenOriginalAsync(
            adminPhotoRepository,
            galleryPhotoBlobService,
            picId,
            cancellationToken);
        if (original is null)
        {
            return NotFound();
        }

        return File(original.Stream, original.ContentType);
    }
}
