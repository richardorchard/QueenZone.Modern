using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class VersionedStaticPathTests
{
    [Fact]
    public void TryGetVersionedWebpSrc_returnsVersionedPathWhenWebpExists()
    {
        var environment = new TestWebHostEnvironment
        {
            WebRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
        };
        Directory.CreateDirectory(Path.Combine(environment.WebRootPath, "design-system", "assets"));
        var webpPath = Path.Combine(environment.WebRootPath, "design-system", "assets", "img-hero.webp");
        File.WriteAllText(webpPath, "webp");

        var result = VersionedStaticPath.TryGetVersionedWebpSrc(
            new TestFileVersionProvider(),
            environment,
            PathString.Empty,
            "/design-system/assets/img-hero.jpg");

        Assert.Equal("/design-system/assets/img-hero.webp?v=test-hash", result);
    }

    [Fact]
    public void TryGetVersionedWebpSrc_returnsNullWhenWebpMissing()
    {
        var environment = new TestWebHostEnvironment
        {
            WebRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
        };
        Directory.CreateDirectory(Path.Combine(environment.WebRootPath, "design-system", "assets"));

        var result = VersionedStaticPath.TryGetVersionedWebpSrc(
            new TestFileVersionProvider(),
            environment,
            PathString.Empty,
            "/design-system/assets/crest-white.png");

        Assert.Null(result);
    }

    [Fact]
    public void TryGetVersionedWebpSrc_usesKnownPhotoFallbackWhenWebRootMissing()
    {
        var environment = new TestWebHostEnvironment { WebRootPath = null! };

        var result = VersionedStaticPath.TryGetVersionedWebpSrc(
            new TestFileVersionProvider(),
            environment,
            PathString.Empty,
            "/design-system/assets/img-stage.jpg");

        Assert.Equal("/design-system/assets/img-stage.webp?v=test-hash", result);
    }

    private sealed class TestFileVersionProvider : IFileVersionProvider
    {
        public string AddFileVersionToPath(PathString requestPathBase, string path) => $"{path}?v=test-hash";
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "QueenZone.Web.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
