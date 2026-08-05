using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Playwright;

namespace QueenZone.Web.E2E;

/// <summary>
/// Sitemap-driven sweep (#545) over every public section, asserting page *shape* rather than
/// content, so it stays correct as the real-data archive grows and changes. Discovers URLs at
/// runtime from <c>/sitemap.xml</c> instead of hardcoding record ids.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category(E2ECategories.RealData)]
[Category(E2ECategories.ReadOnly)]
public class SitemapPublicRouteSweepTests : RealDataPageTest
{
    private const int DefaultPerSectionSampleCap = 5;

    private static readonly string[] StaticExtraRoutes =
    [
        "/about",
        "/privacy",
        "/terms",
        "/data-deletion",
        "/search",
        "/links",
    ];

    private static readonly Regex EncodingArtifactPattern = new(
        @"&(amp|lt|gt|quot|apos|\#39|nbsp);",
        RegexOptions.Compiled);

    protected override bool AllowsWrites => false;

    [Test]
    public async Task SitemapDiscoveredAndStaticUrls_MeetShapeAssertionsAsync()
    {
        using var httpClient = CreateHttpClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var sectionedUrls = await DiscoverSampledSectionUrlsAsync(httpClient, cts.Token);

        var urls = sectionedUrls.Values
            .SelectMany(v => v)
            .Concat(StaticExtraRoutes)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(u => u, StringComparer.Ordinal)
            .ToList();

        TestContext.Out.WriteLine($"Sitemap sweep (seed {SeededSampler.Seed}): {urls.Count} URLs");
        foreach (var url in urls)
        {
            TestContext.Out.WriteLine($"  {url}");
        }

        var consoleErrors = new List<string>();
        void OnConsoleMessage(object? _, IConsoleMessage message)
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                consoleErrors.Add(message.Text);
            }
        }

        await using var mobileContext = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
        });
        var mobilePage = await mobileContext.NewPageAsync();

        var failures = new List<string>();
        Page.Console += OnConsoleMessage;
        try
        {
            foreach (var url in urls)
            {
                await AssertUrlShapeAsync(url, mobilePage, consoleErrors, failures);
            }
        }
        finally
        {
            Page.Console -= OnConsoleMessage;
        }

        Assert.That(
            failures,
            Is.Empty,
            "Sitemap sweep failures:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Test]
    public async Task UnknownPath_Returns404WithStyledNotFoundPageAsync()
    {
        var response = await Page.GotoAsync("/this-page-does-not-exist-e2e-545");

        Assert.That(response?.Status, Is.EqualTo(404));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Page Not Found", Level = 1 }))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task StaleSlug_RedirectsToCanonicalUrlAsync()
    {
        var response = await Page.GotoAsync("/news/1003/wrong-slug-here");

        Assert.That(response?.Request.RedirectedFrom, Is.Not.Null, "Expected the stale slug to redirect.");
        await Expect(Page).ToHaveURLAsync(new Regex(".*/news/1003/queenzone-modernisation-begins/?$"));
    }

    [Test]
    public async Task RepresentativeSectionPages_HaveNoCriticalAxeViolationsAsync()
    {
        using var httpClient = CreateHttpClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var sectionedUrls = await DiscoverSampledSectionUrlsAsync(httpClient, cts.Token);

        var failures = new List<string>();
        foreach (var (section, urls) in sectionedUrls)
        {
            var representative = urls.FirstOrDefault();
            if (representative is null)
            {
                continue;
            }

            await Page.GotoAsync(representative);
            try
            {
                await AxeAssertions.AssertNoCriticalViolationsAsync(Page);
            }
            catch (Exception ex)
            {
                failures.Add($"{section} ({representative}): {ex.Message}");
            }
        }

        Assert.That(
            failures,
            Is.Empty,
            "Axe critical violations by section:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    private async Task AssertUrlShapeAsync(string url, IPage mobilePage, List<string> consoleErrors, List<string> failures)
    {
        consoleErrors.Clear();

        IResponse? response;
        try
        {
            response = await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        }
        catch (Exception ex)
        {
            failures.Add($"{url}: navigation failed - {ex.Message}");
            return;
        }

        if (response is null || response.Status != 200)
        {
            failures.Add($"{url}: expected HTTP 200, got {(response is null ? "no response" : response.Status.ToString())}");
            return;
        }

        var h1Locator = Page.Locator("h1");
        var h1Count = await h1Locator.CountAsync();
        if (h1Count != 1)
        {
            failures.Add($"{url}: expected exactly one <h1>, found {h1Count}");
        }
        else if (string.IsNullOrWhiteSpace(await h1Locator.InnerTextAsync()))
        {
            failures.Add($"{url}: <h1> is empty");
        }

        var canonicalLocator = Page.Locator("link[rel='canonical']");
        var canonicalCount = await canonicalLocator.CountAsync();
        if (canonicalCount != 1)
        {
            failures.Add($"{url}: expected exactly one canonical link, found {canonicalCount}");
        }
        else
        {
            var href = await canonicalLocator.GetAttributeAsync("href");
            if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(href, UriKind.Absolute, out var canonicalUri))
            {
                failures.Add($"{url}: canonical href is missing or not absolute ('{href}')");
            }
            else if (!string.Equals(canonicalUri.PathAndQuery.TrimEnd('/'), url.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{url}: canonical href '{canonicalUri.PathAndQuery}' does not match requested path");
            }
        }

        if (consoleErrors.Count > 0)
        {
            failures.Add($"{url}: {consoleErrors.Count} browser console error(s): {string.Join(" | ", consoleErrors)}");
        }

        await mobilePage.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        var overflows = await mobilePage.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        if (overflows)
        {
            failures.Add($"{url}: horizontal overflow at 390px viewport width");
        }

        var bodyText = await Page.Locator("body").InnerTextAsync();
        var artifactMatch = EncodingArtifactPattern.Match(bodyText);
        if (artifactMatch.Success)
        {
            failures.Add($"{url}: unrendered HTML-encoding artifact in visible text: '{artifactMatch.Value}'");
        }
    }

    private static async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> DiscoverSampledSectionUrlsAsync(
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var perSectionCap = ResolvePerSectionSampleCap();

        var indexXml = await httpClient.GetStringAsync("/sitemap.xml", cancellationToken);
        var childSitemapPaths = ParseLocPaths(indexXml);

        var sectioned = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var sitemapPath in childSitemapPaths)
        {
            var section = ResolveSectionName(sitemapPath);
            var childXml = await httpClient.GetStringAsync(sitemapPath, cancellationToken);
            var urlPaths = ParseLocPaths(childXml);

            if (!sectioned.TryGetValue(section, out var list))
            {
                list = [];
                sectioned[section] = list;
            }

            list.AddRange(urlPaths);
        }

        var sampled = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var (section, urls) in sectioned)
        {
            sampled[section] = SeededSampler.SampleFirstLastAndRandom(urls, perSectionCap);
        }

        return sampled;
    }

    private static int ResolvePerSectionSampleCap() =>
        int.TryParse(Environment.GetEnvironmentVariable("E2E_SITEMAP_SWEEP_PER_SECTION_LIMIT"), out var value) && value > 0
            ? value
            : DefaultPerSectionSampleCap;

    private static string ResolveSectionName(string sitemapPath)
    {
        var fileName = sitemapPath.TrimStart('/');
        if (string.Equals(fileName, "sitemap-core.xml", StringComparison.OrdinalIgnoreCase))
        {
            return "core";
        }

        var forumMatch = Regex.Match(fileName, @"^sitemap-forum-\d+\.xml$", RegexOptions.IgnoreCase);
        if (forumMatch.Success)
        {
            return "forum";
        }

        var sectionMatch = Regex.Match(fileName, @"^sitemap-(.+)\.xml$", RegexOptions.IgnoreCase);
        return sectionMatch.Success ? sectionMatch.Groups[1].Value : fileName;
    }

    private static IReadOnlyList<string> ParseLocPaths(string xml)
    {
        var doc = XDocument.Parse(xml);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        return doc.Descendants(ns + "loc")
            .Select(e => e.Value.Trim())
            .Where(v => v.Length > 0)
            .Select(ToPath)
            .ToList();
    }

    private static string ToPath(string locValue) =>
        Uri.TryCreate(locValue, UriKind.Absolute, out var uri) ? uri.PathAndQuery : locValue;

    private HttpClient CreateHttpClient() => new() { BaseAddress = new Uri(BaseUrl) };
}
