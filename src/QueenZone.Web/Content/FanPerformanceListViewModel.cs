using System.Text.Encodings.Web;
using System.Text.Json;
using QueenZone.Data;

namespace QueenZone.Web;

public sealed record FanPerformanceListItem(
    int Id,
    string Title,
    string PerformedBy,
    string Description,
    DateTime DateAdded,
    string? AudioPlayPath);

public sealed record FanPerformanceCatalogEntry(int Id, string Title, string AudioPlayPath);

public sealed record FanPerformanceListViewModel(
    IReadOnlyList<FanPerformanceListItem> Items,
    string LoginReturnUrl,
    IReadOnlyList<FanPerformanceCatalogEntry> Catalog)
{
    private static readonly JsonSerializerOptions CatalogJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Default
    };

    public static FanPerformanceListViewModel Empty { get; } = new([], FanPerformanceRoutes.GetIndexPath(), []);

    public bool CanPlayCatalog => Catalog.Count > 0;

    public string CatalogJson => JsonSerializer.Serialize(Catalog, CatalogJsonOptions);

    public static IReadOnlyList<FanPerformanceCatalogEntry> CreateCatalog(IReadOnlyList<FanPerformance> performances) =>
        performances
            .Select(performance => new FanPerformanceCatalogEntry(
                performance.Id,
                performance.Title,
                FanPerformanceRoutes.GetAudioPath(performance.Id, performance.Title)))
            .ToList();
}
