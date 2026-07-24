using QueenZone.Data;
using QueenZone.Storage;
using QueenZone.Web;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace QueenZone.Web.Tests;

public sealed class InMemoryAdminPhotoRepositoryTests
{
    [Fact]
    public async Task Create_Update_Hide_Move_And_Delete_WorkAgainstSharedStore()
    {
        var store = new SharedPhotoStore(SamplePhotoData.CreateSeedCategories());
        var admin = new InMemoryAdminPhotoRepository(store);
        var publicRepo = new InMemoryPhotoRepository(store);

        var createdId = await admin.CreateAsync(
            new AdminPhotoCreateRequest(
                CatId: 9,
                Title: "Admin uploaded",
                Keywords: "brian,guitar",
                Year: 1990,
                DateTime: new DateTime(1990, 1, 2),
                IsVisible: true,
                LegacyUrl: "/Brian_May/admin-new.jpg",
                LegacyThumbUrl: "/Brian_May/admin-new_t.webp",
                ThumbWidth: 400,
                ThumbHeight: 400,
                PictureWidth: 1200,
                PictureHeight: 800),
            "admin@test.local");

        var created = await admin.GetByIdAsync(createdId);
        Assert.NotNull(created);
        Assert.Equal("Admin uploaded", created.Title);
        Assert.True(created.IsVisible);

        var publicItems = await publicRepo.GetCategoryAllAsync(9);
        Assert.Contains(publicItems, item => item.PicId == createdId);

        await admin.UpdateAsync(
            createdId,
            new AdminPhotoUpdateRequest("Renamed", "live", 1991, new DateTime(1991, 2, 3), CatId: 12),
            "admin@test.local");

        var moved = await admin.GetByIdAsync(createdId);
        Assert.NotNull(moved);
        Assert.Equal(12, moved.CatId);
        Assert.Equal("Queen", moved.CategoryName);
        Assert.Equal("Renamed", moved.Title);

        await admin.SetVisibilityAsync(createdId, false, "admin@test.local");
        var hiddenPublic = await publicRepo.GetCategoryAllAsync(12);
        Assert.DoesNotContain(hiddenPublic, item => item.PicId == createdId);

        var adminList = await admin.GetPageAsync(new AdminPhotoListFilter(IsVisible: false), 1, 50);
        Assert.Contains(adminList.Items, item => item.PicId == createdId);

        await admin.DeleteAsync(createdId, "admin@test.local");
        Assert.Null(await admin.GetByIdAsync(createdId));
    }

    [Fact]
    public async Task SearchFilter_MatchesTitleAndKeywords()
    {
        var store = new SharedPhotoStore(SamplePhotoData.CreateSeedCategories());
        var admin = new InMemoryAdminPhotoRepository(store);

        await admin.UpdateAsync(
            101,
            new AdminPhotoUpdateRequest("Brian in action with his guitar", "red-special", 1986, new DateTime(1986, 7, 12), 9),
            "admin@test.local");

        var byKeyword = await admin.GetPageAsync(new AdminPhotoListFilter(Search: "red-special"), 1, 20);
        Assert.Contains(byKeyword.Items, item => item.PicId == 101);

        var byTitle = await admin.GetPageAsync(new AdminPhotoListFilter(Search: "Wembley"), 1, 20);
        Assert.Contains(byTitle.Items, item => item.Title.Contains("Wembley", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class AdminPhotoServiceTests
{
    [Fact]
    public async Task Create_UploadsOriginalAndThumb_ThenInsertsRow()
    {
        var store = new SharedPhotoStore(SamplePhotoData.CreateSeedCategories());
        var admin = new InMemoryAdminPhotoRepository(store);
        var blobs = new NullGalleryPhotoBlobService();
        var service = new AdminPhotoService(admin, blobs);

        await using var imageStream = await CreateJpegAsync(640, 480);
        var file = new FormFile(imageStream, 0, imageStream.Length, "file", "shot.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg",
        };

        var picId = await service.CreateAsync(
            file,
            catId: 9,
            title: "Service upload",
            keywords: "test",
            year: 2024,
            dateTime: new DateTime(2024, 5, 1),
            isVisible: true,
            editorEmail: "admin@test.local");

        var photo = await admin.GetByIdAsync(picId);
        Assert.NotNull(photo);
        Assert.Equal("Service upload", photo.Title);
        Assert.Contains("_t.webp", photo.LegacyThumbUrl, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("/Brian_May/", photo.LegacyUrl, StringComparison.Ordinal);

        var blobUrl = PhotoImageUrl.ToBlobStorageUrl(photo.LegacyUrl);
        Assert.True(PhotoImageUrl.TryParseBlobLocation(blobUrl, out var container, out var blobName));
        await using var original = await blobs.OpenReadAsync(container, blobName);
        Assert.NotNull(original);
    }

    [Fact]
    public async Task RegenerateThumbnail_UpdatesThumbMetadata()
    {
        var store = new SharedPhotoStore(SamplePhotoData.CreateSeedCategories());
        var admin = new InMemoryAdminPhotoRepository(store);
        var blobs = new NullGalleryPhotoBlobService();
        var service = new AdminPhotoService(admin, blobs);

        await using var imageStream = await CreateJpegAsync(500, 500);
        var file = new FormFile(imageStream, 0, imageStream.Length, "file", "regen.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg",
        };

        var picId = await service.CreateAsync(
            file,
            catId: 9,
            title: "Regen target",
            keywords: null,
            year: 2020,
            dateTime: DateTime.UtcNow,
            isVisible: true,
            editorEmail: "admin@test.local");

        await service.RegenerateThumbnailAsync(picId, "admin@test.local");

        var photo = await admin.GetByIdAsync(picId);
        Assert.NotNull(photo);
        Assert.Equal(400, photo.ThumbWidth);
        Assert.Equal(400, photo.ThumbHeight);
        Assert.EndsWith("_t.webp", photo.LegacyThumbUrl, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<MemoryStream> CreateJpegAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        await image.SaveAsJpegAsync(stream, new JpegEncoder { Quality = 80 });
        stream.Position = 0;
        return stream;
    }
}

public sealed class PhotoLegacyPathTests
{
    [Fact]
    public void CategoryFolder_And_Container_MatchCdnConventions()
    {
        Assert.Equal("Brian_May", PhotoLegacyPath.CategoryFolder("Brian May"));
        Assert.Equal("brian-may", PhotoLegacyPath.BlobContainerName("Brian May"));
        Assert.Equal("/Brian_May/abc.jpg", PhotoLegacyPath.BuildLegacyPath("Brian May", "abc.jpg"));
    }

    [Fact]
    public void UsQueenConvention_UsesShortenedBlobContainer()
    {
        Assert.Equal("US_Queen_Convention_2001", PhotoLegacyPath.CategoryFolder("US Queen Convention 2001"));
        Assert.Equal("us-convention-2001", PhotoLegacyPath.BlobContainerName("US Queen Convention 2001"));
        Assert.Equal(
            "/US_Queen_Convention_2001/121120014455.jpg",
            PhotoLegacyPath.BuildLegacyPath("US Queen Convention 2001", "121120014455.jpg"));
    }
}
