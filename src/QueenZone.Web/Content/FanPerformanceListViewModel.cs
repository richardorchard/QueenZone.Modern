namespace QueenZone.Web;

public sealed record FanPerformanceListItem(
    int Id,
    string Title,
    string PerformedBy,
    string Description,
    DateTime DateAdded,
    string? AudioPlayPath);

public sealed record FanPerformanceListViewModel(
    IReadOnlyList<FanPerformanceListItem> Items,
    string LoginReturnUrl)
{
    public static FanPerformanceListViewModel Empty { get; } = new([], FanPerformanceRoutes.GetIndexPath());
}
