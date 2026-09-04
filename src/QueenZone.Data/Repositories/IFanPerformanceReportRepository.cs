namespace QueenZone.Data;

public interface IFanPerformanceReportRepository
{
    Task<FanPerformanceReportCreateResult> CreateAsync(
        NewFanPerformanceReport report,
        CancellationToken cancellationToken = default);

    Task<FanPerformanceReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FanPerformanceReportListPage> ListAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<FanPerformanceReport?> UpdateStatusAsync(
        Guid id,
        string status,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<int> CountOpenAsync(CancellationToken cancellationToken = default);
}
