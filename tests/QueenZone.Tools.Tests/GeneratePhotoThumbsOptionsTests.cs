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
    public void Parse_reads_pic_ids_file_and_thumb_size()
    {
        var path = Path.Combine(Path.GetTempPath(), $"qz-pic-ids-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, """
            # comment
            17816
            17817
            -- skip
            """);

        try
        {
            var options = GeneratePhotoThumbsOptions.Parse(
            [
                "--pic-ids-file", path,
                "--connection-string", "Server=.;Database=x;",
                "--storage-connection-string", "UseDevelopmentStorage=true",
                "--thumb-size", "320",
            ]);

            Assert.True(options.IsValid);
            Assert.Equal([17816, 17817], options.PicIds);
            Assert.Equal(320, options.ThumbSizePixels);
            Assert.False(options.DryRun);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Parse_rejects_invalid_thumb_size_and_missing_storage()
    {
        var badSize = GeneratePhotoThumbsOptions.Parse(
        [
            "--pic-ids", "1",
            "--connection-string", "Server=.;Database=x;",
            "--thumb-size", "0",
        ]);
        Assert.False(badSize.IsValid);

        var settingsPath = WriteSettingsFile("""{ "ConnectionStrings": { "QueenZoneLegacyLive": "Server=.;Database=x;" } }""");
        try
        {
            var missingStorage = GeneratePhotoThumbsOptions.Parse(
            [
                "--pic-ids", "1",
                "--settings-file", settingsPath,
            ]);
            Assert.False(missingStorage.IsValid);
            Assert.Contains("blob storage", missingStorage.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(settingsPath);
        }
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

    [Fact]
    public async Task RunAsync_returns_usage_error_for_invalid_options()
    {
        var exitCode = await GeneratePhotoThumbsCommand.RunAsync(["--dry-run"]);
        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task RunCoreAsync_returns_zero_for_empty_and_dry_run()
    {
        var options = GeneratePhotoThumbsOptions.Parse(
        [
            "--pic-ids", "17816",
            "--connection-string", "Server=.;Database=x;",
            "--dry-run",
        ]);

        Assert.Equal(0, await GeneratePhotoThumbsCommand.RunCoreAsync(options, []));

        var photos = new[]
        {
            new GalleryPhotoRow(17816, "/Freddie_Mercury/celeb2.jpg", "/Freddie_Mercury/celeb2.jpg", 1),
        };
        Assert.Equal(0, await GeneratePhotoThumbsCommand.RunCoreAsync(options, photos));
    }

    [Fact]
    public async Task RunCoreAsync_reports_success_and_failure_from_generator()
    {
        var options = GeneratePhotoThumbsOptions.Parse(
        [
            "--pic-ids", "1,2",
            "--connection-string", "Server=.;Database=x;",
            "--storage-connection-string", "UseDevelopmentStorage=true",
        ]);

        var photos = new[]
        {
            new GalleryPhotoRow(1, "/Freddie_Mercury/a.jpg", "/Freddie_Mercury/a.jpg", 1),
            new GalleryPhotoRow(2, "/Freddie_Mercury/b.jpg", "/Freddie_Mercury/b.jpg", 1),
        };

        var generator = new StubGalleryThumbGenerator(photo =>
        {
            if (photo.PicId == 2)
            {
                throw new InvalidOperationException("boom");
            }

            return new GeneratedThumbResult("/Freddie_Mercury/a_t.webp", 400, 400);
        });

        var exitCode = await GeneratePhotoThumbsCommand.RunCoreAsync(options, photos, generator);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ToolsApp_dispatches_generate_photo_thumbs()
    {
        var exitCode = await ToolsApp.RunAsync(["generate-photo-thumbs", "--dry-run"]);
        Assert.Equal(2, exitCode);
    }

    private static string WriteSettingsFile(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"qz-settings-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, contents);
        return path;
    }

    private sealed class StubGalleryThumbGenerator(Func<GalleryPhotoRow, GeneratedThumbResult> responder)
        : IGalleryThumbGenerator
    {
        public Task<GeneratedThumbResult> GenerateAsync(GalleryPhotoRow photo, GeneratePhotoThumbsOptions options) =>
            Task.FromResult(responder(photo));
    }
}
