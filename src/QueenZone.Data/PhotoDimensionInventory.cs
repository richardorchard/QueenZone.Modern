namespace QueenZone.Data;

/// <summary>
/// Read-only aggregate of original image dimensions for public photos
/// (<c>PIC_WIDTH</c> / <c>PIC_HEIGHT</c>). Used by ops inventory tooling (#435).
/// </summary>
public sealed record PhotoDimensionInventoryReport(
    int TotalPublic,
    int UsableDimensions,
    int MissingOrZeroDimensions,
    int DesktopWallpaperCandidates,
    int PhoneWallpaperCandidates,
    int LargeCandidates,
    int LandscapeUsable,
    int PortraitUsable,
    int SquareUsable,
    IReadOnlyList<PhotoDimensionBucket> LongestSideBuckets)
{
    public double UsablePercent => TotalPublic == 0 ? 0 : 100.0 * UsableDimensions / TotalPublic;

    public double MissingPercent => TotalPublic == 0 ? 0 : 100.0 * MissingOrZeroDimensions / TotalPublic;
}

public sealed record PhotoDimensionBucket(string Label, int Count);

/// <summary>
/// Pure aggregation over original width/height pairs (zeros = unknown).
/// </summary>
public static class PhotoDimensionInventory
{
    /// <summary>Draft wallpaper thresholds from epic #434 (original size).</summary>
    public const int DesktopMinWidth = 1920;

    public const int PhoneMinHeight = 1920;

    public const int LargeMinLongestSide = 1920;

    private static readonly (string Label, int MinInclusive, int MaxExclusive)[] LongestSideRanges =
    [
        ("0 (unknown/zero longest)", 0, 1),
        ("1–799", 1, 800),
        ("800–1279", 800, 1280),
        ("1280–1919", 1280, 1920),
        ("1920–2559", 1920, 2560),
        ("2560+", 2560, int.MaxValue),
    ];

    public static PhotoDimensionInventoryReport FromDimensions(IEnumerable<(int Width, int Height)> dimensions)
    {
        var total = 0;
        var usable = 0;
        var missing = 0;
        var desktop = 0;
        var phone = 0;
        var large = 0;
        var landscape = 0;
        var portrait = 0;
        var square = 0;
        var bucketCounts = new int[LongestSideRanges.Length];

        foreach (var (width, height) in dimensions)
        {
            total++;
            var w = Math.Max(0, width);
            var h = Math.Max(0, height);
            var hasUsable = w > 0 && h > 0;
            if (!hasUsable)
            {
                missing++;
                bucketCounts[0]++;
                continue;
            }

            usable++;
            var longest = Math.Max(w, h);
            for (var i = 1; i < LongestSideRanges.Length; i++)
            {
                var range = LongestSideRanges[i];
                if (longest >= range.MinInclusive && longest < range.MaxExclusive)
                {
                    bucketCounts[i]++;
                    break;
                }
            }

            if (w > h)
            {
                landscape++;
            }
            else if (h > w)
            {
                portrait++;
            }
            else
            {
                square++;
            }

            if (w >= DesktopMinWidth && w >= h)
            {
                desktop++;
            }

            if (h >= PhoneMinHeight && h > w)
            {
                phone++;
            }

            if (longest >= LargeMinLongestSide)
            {
                large++;
            }
        }

        IReadOnlyList<PhotoDimensionBucket> buckets = LongestSideRanges
            .Select((range, index) => new PhotoDimensionBucket(range.Label, bucketCounts[index]))
            .ToList();

        return new PhotoDimensionInventoryReport(
            total,
            usable,
            missing,
            desktop,
            phone,
            large,
            landscape,
            portrait,
            square,
            buckets);
    }

    public static PhotoDimensionInventoryReport FromPhotos(IEnumerable<PhotoItem> photos) =>
        FromDimensions(photos.Select(photo => (photo.PictureWidth, photo.PictureHeight)));

    public static string FormatText(PhotoDimensionInventoryReport report)
    {
        var lines = new List<string>
        {
            "Photo original-dimension inventory (DISPLAY = 1 / public photos)",
            $"Total public photos: {report.TotalPublic:N0}",
            $"Usable dims (W>0 and H>0): {report.UsableDimensions:N0} ({report.UsablePercent:0.##}%)",
            $"Missing/zero dims: {report.MissingOrZeroDimensions:N0} ({report.MissingPercent:0.##}%)",
            string.Empty,
            "Draft wallpaper candidates (original size; see epic #434):",
            $"  Desktop (landscape, width >= {DesktopMinWidth}): {report.DesktopWallpaperCandidates:N0}",
            $"  Phone (portrait, height >= {PhoneMinHeight}): {report.PhoneWallpaperCandidates:N0}",
            $"  Large (longest side >= {LargeMinLongestSide}): {report.LargeCandidates:N0}",
            string.Empty,
            "Orientation among usable rows:",
            $"  Landscape: {report.LandscapeUsable:N0}",
            $"  Portrait: {report.PortraitUsable:N0}",
            $"  Square: {report.SquareUsable:N0}",
            string.Empty,
            "Longest-side buckets:",
        };

        foreach (var bucket in report.LongestSideBuckets)
        {
            lines.Add($"  {bucket.Label}: {bucket.Count:N0}");
        }

        lines.Add(string.Empty);
        lines.Add(RecommendNextStep(report));
        return string.Join(Environment.NewLine, lines);
    }

    public static string RecommendNextStep(PhotoDimensionInventoryReport report)
    {
        if (report.TotalPublic == 0)
        {
            return "Recommendation: no public photos found; inventory not actionable.";
        }

        if (report.UsablePercent < 50)
        {
            return "Recommendation: prioritize backfill (#438) before relying on wallpaper filters; " +
                   "display (#436) can still ship (shows dims when known).";
        }

        if (report.DesktopWallpaperCandidates + report.PhoneWallpaperCandidates == 0)
        {
            return "Recommendation: usable dims exist but draft wallpaper thresholds match few/no rows — " +
                   "lower thresholds for #437 or complete backfill of large scans.";
        }

        if (report.MissingPercent > 15)
        {
            return "Recommendation: ship category filters (#437) with current data; " +
                   "schedule optional backfill (#438) to improve recall of zero-dim rows.";
        }

        return "Recommendation: coverage is healthy enough to ship filters (#437) with current thresholds; " +
               "backfill (#438) is optional polish.";
    }
}
