using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class GalleryPickerModel(IAdminPhotoRepository adminPhotoRepository) : AdminNewsPageModel
{
    [BindProperty(SupportsGet = true)]
    public int? CatId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public AdminPhotoPage? Photos { get; private set; }

    public IReadOnlyList<AdminPhotoCategory> Categories { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Categories = await adminPhotoRepository.GetCategoriesAsync(cancellationToken);
        var page = Math.Max(PageNumber, 1);
        var photos = await adminPhotoRepository.GetPageAsync(
            NewsArticleGalleryPicker.BuildFilter(CatId, Q),
            page,
            NewsArticleGalleryPicker.PageSize,
            cancellationToken);
        var usable = photos.Items.Where(IsLargeEnoughForCard).ToList();
        Photos = usable.Count == photos.Items.Count
            ? photos
            : photos with { Items = usable };
        ViewData["Title"] = "Choose a gallery photo";
    }

    /// <summary>
    /// True when the photo is large enough to yield a 3:2 crop that meets
    /// <see cref="NewsArticleImageProcessor.MinCropWidth"/>/<see cref="NewsArticleImageProcessor.MinCropHeight"/>.
    /// Legacy rows whose <c>PIC_WIDTH</c>/<c>PIC_HEIGHT</c> were never backfilled (issue #438)
    /// report 0x0; treat those as unknown rather than too small, since the actual crop is
    /// validated against the real image bytes when the photo is chosen.
    /// </summary>
    private static bool IsLargeEnoughForCard(AdminPhotoItem item)
    {
        if (item.PictureWidth <= 0 || item.PictureHeight <= 0)
        {
            return true;
        }

        var maxCrop = NewsArticleImageProcessor.CenterCardCrop(item.PictureWidth, item.PictureHeight);
        return maxCrop.Width >= NewsArticleImageProcessor.MinCropWidth
            && maxCrop.Height >= NewsArticleImageProcessor.MinCropHeight;
    }
}
