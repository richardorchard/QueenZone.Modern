using QueenZone.Tools;

namespace QueenZone.Tools.Tests;

public sealed class GeneratePhotoThumbsOptionsTests
{
    [Fact]
    public void Parse_requires_pic_ids()
    {
        var options = GeneratePhotoThumbsOptions.Parse(["--dry-run"]);
        Assert.False(options.IsValid);
        Assert.Contains("pic-ids", options.ErrorMessage);
    }

    [Fact]
    public void Parse_reads_pic_ids_and_dry_run()
    {
        var options = GeneratePhotoThumbsOptions.Parse(
        [
            "--pic-ids", "17816,17817",
            "--connection-string", "Server=.;Database=x;Trusted_Connection=True;",
            "--dry-run",
        ]);

        Assert.True(options.IsValid);
        Assert.True(options.DryRun);
        Assert.Equal([17816, 17817], options.PicIds);
    }

    [Fact]
    public void PlanThumbnail_maps_legacy_url_to_webp_thumb()
    {
        var photo = new GalleryPhotoRow(
            17816,
            "/Freddie_Mercury/celeb2.jpg",
            "/Freddie_Mercury/celeb2.jpg",
            1);

        var plan = GeneratePhotoThumbsCommand.PlanThumbnail(photo, 400);

        Assert.Equal("freddie-mercury", plan.Container);
        Assert.Equal("celeb2.jpg", plan.SourceBlobName);
        Assert.Equal("celeb2_t.webp", plan.ThumbnailBlobName);
        Assert.Equal("/Freddie_Mercury/celeb2_t.webp", plan.LegacyThumbPath);
    }
}
