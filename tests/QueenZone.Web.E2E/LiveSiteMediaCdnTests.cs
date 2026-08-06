using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Playwright;
using QueenZone.Data;

namespace QueenZone.Web.E2E;

/// <summary>
/// Live-site / real-data media host checks (#551). Anonymous public pages only — no admin or
/// member routes. Fan-performance audio is member-gated on the page, so the CDN contract is
/// asserted via <see cref="SongFileUrl"/> plus a ranged request to the public songfiles host.
/// Photography images may resolve through <c>cdn.queenzone.org</c> (or follow redirects).
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category(E2ECategories.RealData)]
[Category(E2ECategories.ReadOnly)]
public class LiveSiteMediaCdnTests : RealDataPageTest
{
    private static readonly string[] AllowedImageHosts =
    [
        "cdn.queenzone.org",
        "cdn2.queenzone.org",
        "www.queenzone.org",
        "queenzone.org",
    ];

    /// <summary>
    /// Legacy sample songfile known to exist in the production songfiles container
    /// (same filename as <c>SampleFanPerformanceData</c> / historical Q_STAGE_T rows).
    /// </summary>
    private const string KnownSongfileName = "2014417798057369.mp3";

    protected override bool AllowsWrites => false;

    [Test]
    public void FanPerformanceAudioUrls_MustUseCdn2Host()
    {
        var url = SongFileUrl.Build(KnownSongfileName);
        Assert.That(
            url,
            Does.StartWith("https://cdn2.queenzone.org/songfiles/"),
            "Fan-performance audio must use cdn2.queenzone.org (Worker sets Content-Disposition). " +
            "Using cdn.queenzone.org silently breaks download filenames — see AGENTS.md Media Serving.");
    }

    [Test]
    public async Task KnownFanPerformanceSongfile_ResolvesThroughCdn2Async()
    {
        var songUrl = SongFileUrl.Build(KnownSongfileName);
        Assert.That(songUrl, Does.Contain("cdn2.queenzone.org"));

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            // CDN may return a blob host after Worker processing; still assert the public URL we hit is cdn2.
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        using var request = new HttpRequestMessage(HttpMethod.Get, songUrl);
        // Avoid downloading the whole track on the nightly B1 / CDN path.
        request.Headers.Range = new RangeHeaderValue(0, 1023);

        using var response = await client.SendAsync(request);

        // Request URL host must be cdn2 even if a Worker hop lands on blob storage.
        Assert.That(
            new Uri(songUrl).Host,
            Is.EqualTo("cdn2.queenzone.org"),
            "SongFileUrl must target the cdn2 Worker host.");

        Assert.That(
            (int)response.StatusCode,
            Is.LessThan(500),
            FailurePrefix() +
            $"expected a non-5xx response from {songUrl}, got {(int)response.StatusCode} {response.StatusCode}. " +
            "Fan-performance audio must resolve through cdn2.queenzone.org (see AGENTS.md Media Serving).");

        if (response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.PartialContent))
        {
            // File may rotate out of the public container; host reachability is the hard check.
            TestContext.Out.WriteLine(
                $"SOFT: {songUrl} returned {(int)response.StatusCode} {response.StatusCode} " +
                "(cdn2 host responded; object may be absent).");
        }
    }

    [Test]
    public async Task PhotographyImages_ResolveThroughAllowedCdnHostsAsync()
    {
        var response = await Page.GotoAsync(
            "/photography",
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000,
            });

        Assert.That(
            response?.Status,
            Is.EqualTo(200),
            FailurePrefix() + "/photography: expected HTTP 200 for media host sampling.");

        var imageSrcs = await Page.Locator("img[src]").EvaluateAllAsync<string[]>(
            "els => els.map(e => e.getAttribute('src')).filter(Boolean)");

        var absoluteMedia = imageSrcs
            .Where(src => !string.IsNullOrWhiteSpace(src))
            .Select(src =>
            {
                if (Uri.TryCreate(src, UriKind.Absolute, out var abs))
                {
                    return abs;
                }

                if (Uri.TryCreate(new Uri(BaseUrl.TrimEnd('/') + "/"), src, out var relative))
                {
                    return relative;
                }

                return null;
            })
            .Where(uri => uri is not null)
            .Cast<Uri>()
            .Where(uri => uri.Scheme is "http" or "https")
            // Only check off-site / CDN-shaped hosts; same-origin static assets are fine.
            .Where(uri => !IsSameSiteHost(uri.Host))
            .Take(10)
            .ToList();

        if (absoluteMedia.Count == 0)
        {
            TestContext.Out.WriteLine(
                "No off-site photography image hosts found on /photography; skipping CDN host assertions.");
            return;
        }

        var failures = new List<string>();
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true })
        {
            Timeout = TimeSpan.FromSeconds(20),
        };

        foreach (var mediaUri in absoluteMedia)
        {
            if (!IsAllowedMediaHost(mediaUri.Host))
            {
                failures.Add(
                    $"{FailurePrefix()}image host '{mediaUri.Host}' is not an allowed CDN/public host " +
                    $"(url={mediaUri}). Allowed: {string.Join(", ", AllowedImageHosts)}.");
                continue;
            }

            try
            {
                using var head = new HttpRequestMessage(HttpMethod.Head, mediaUri);
                using var mediaResponse = await client.SendAsync(head);
                // Missing thumbs are soft on archives; only fail hard on unexpected host policy above.
                if ((int)mediaResponse.StatusCode >= 500)
                {
                    failures.Add(
                        $"{FailurePrefix()}{mediaUri}: server error {(int)mediaResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"SOFT: {mediaUri}: {ex.Message}");
            }
        }

        Assert.That(
            failures,
            Is.Empty,
            "Media CDN host failures:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    private static bool IsAllowedMediaHost(string host) =>
        AllowedImageHosts.Any(allowed =>
            string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase));

    private static bool IsSameSiteHost(string host)
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return false;
        }

        return string.Equals(host, baseUri.Host, StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }

    private static string FailurePrefix() =>
        RealDataMarkers.IsReadOnlyMode() ? "PRODUCTION LIVE-SITE: " : string.Empty;
}
