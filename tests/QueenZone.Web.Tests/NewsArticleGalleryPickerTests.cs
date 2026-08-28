using Microsoft.AspNetCore.Http;
using QueenZone.Data;
using QueenZone.Web.Pages.Admin.News;

namespace QueenZone.Web.Tests;

public sealed class NewsArticleGalleryPickerTests
{
    [Fact]
    public void BuildFilter_reuses_admin_photo_search_and_category()
    {
        var filter = NewsArticleGalleryPicker.BuildFilter(9, "  Wembley  ");

        Assert.Equal(9, filter.CatId);
        Assert.Equal("Wembley", filter.Search);
        Assert.Null(filter.IsVisible);
        Assert.Null(filter.Year);
    }

    [Fact]
    public void FileName_uses_legacy_path_basename()
    {
        var photo = new AdminPhotoItem(
            101,
            9,
            "Brian May",
            "brian-may",
            "Brian in action with his guitar",
            "/Brian_May/img-101.jpg",
            "/Brian_May/img-101-t.jpg",
            "https://cdn.queenzone.org/brian-may/img-101.jpg",
            "https://cdn.queenzone.org/brian-may/img-101-t.jpg",
            150,
            150,
            1920,
            1080,
            1986,
            new DateTime(1986, 7, 12),
            null,
            true);

        Assert.Equal("img-101.jpg", NewsArticleGalleryPicker.FileName(photo));
    }

    [Fact]
    public void BuildPath_includes_filter_and_page_query()
    {
        Assert.Equal("/admin/news/gallery-picker", NewsArticleGalleryPicker.BuildPath(null, null, 1));
        Assert.Equal(
            "/admin/news/gallery-picker?catId=9&q=Wembley&pageNumber=2",
            NewsArticleGalleryPicker.BuildPath(9, "Wembley", 2));
    }

    [Fact]
    public async Task ResolvePreviewUrl_uses_gallery_cdn_image()
    {
        var photos = new InMemoryAdminPhotoRepository(new SharedPhotoStore(SamplePhotoData.CreateSeedCategories()));
        var draft = NewsArticleImage.WithGalleryPick(
            new AdminNewsDraft("Title", null, "Excerpt", "Body", DateTime.UtcNow.Date, null),
            101);

        var preview = await NewsArticleGalleryPicker.ResolvePreviewUrlAsync(photos, draft);

        Assert.Equal("https://cdn.queenzone.org/brian-may/img-101.jpg", preview);
    }

    [Fact]
    public async Task ValidatePic_rejects_missing_gallery_row_without_upload()
    {
        var photos = new InMemoryAdminPhotoRepository(new SharedPhotoStore(SamplePhotoData.CreateSeedCategories()));

        var missing = await NewsArticleGalleryPicker.ValidatePicAsync(photos, uploadedFile: null, 99999);
        Assert.Equal("That gallery photo was not found.", missing);

        var found = await NewsArticleGalleryPicker.ValidatePicAsync(photos, uploadedFile: null, 101);
        Assert.Null(found);

        await using var stream = new MemoryStream("x"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "articleImage", "hero.png");
        var skipped = await NewsArticleGalleryPicker.ValidatePicAsync(photos, file, 99999);
        Assert.Null(skipped);
    }

    [Fact]
    public void ToDraft_normalizes_gallery_pick_over_previous_blob_key()
    {
        var form = new AdminNewsForm
        {
            Title = "Gallery pick",
            Excerpt = "Excerpt",
            Body = "Body",
            PublishedAt = "2026-06-14",
            ImageBlobKey = "editors/me/old.webp",
            ImageGalleryPicId = 101
        };

        var draft = form.ToDraft();

        Assert.Equal("gallery:101", draft.ImageBlobKey);
        Assert.Equal(101, draft.ImageGalleryPicId);
    }
}
