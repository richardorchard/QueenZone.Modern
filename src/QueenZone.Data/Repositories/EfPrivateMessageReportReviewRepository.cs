using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfPrivateMessageReportReviewRepository(QueenZoneDbContext dbContext)
    : IPrivateMessageReportReviewRepository
{
    public async Task<PrivateMessageReportListPage> ListReportsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var statusFilter = NormalizeOptionalStatus(status);

        var query = dbContext.PrivateMessageReports.AsNoTracking();
        if (statusFilter is not null)
        {
            query = query.Where(report => report.Status == statusFilter);
        }

        if (IsSqliteDatabase())
        {
            var allRows = await query
                .Select(report => new
                {
                    report.Id,
                    report.ReporterMemberId,
                    ReporterDisplayName = report.Reporter != null ? report.Reporter.DisplayName : null,
                    report.ReportedMemberId,
                    ReportedDisplayName = report.Reported != null ? report.Reported.DisplayName : null,
                    report.Reason,
                    report.CreatedAt,
                    report.Status,
                })
                .ToListAsync(cancellationToken);

            var ordered = allRows.OrderByDescending(row => row.CreatedAt).ToList();
            var items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(row => new PrivateMessageReportListItem(
                    row.Id,
                    row.ReporterMemberId,
                    DisplayNameOrUnknown(row.ReporterDisplayName),
                    row.ReportedMemberId,
                    DisplayNameOrUnknown(row.ReportedDisplayName),
                    row.Reason,
                    row.CreatedAt,
                    row.Status))
                .ToList();

            return new PrivateMessageReportListPage(items, ordered.Count, statusFilter);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pageItems = await query
            .OrderByDescending(report => report.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(report => new PrivateMessageReportListItem(
                report.Id,
                report.ReporterMemberId,
                report.Reporter != null && report.Reporter.DisplayName != null
                    ? report.Reporter.DisplayName
                    : "Unknown member",
                report.ReportedMemberId,
                report.Reported != null && report.Reported.DisplayName != null
                    ? report.Reported.DisplayName
                    : "Unknown member",
                report.Reason,
                report.CreatedAt,
                report.Status))
            .ToListAsync(cancellationToken);

        return new PrivateMessageReportListPage(
            pageItems.Select(item => item with
            {
                ReporterDisplayName = DisplayNameOrUnknown(item.ReporterDisplayName),
                ReportedDisplayName = DisplayNameOrUnknown(item.ReportedDisplayName),
            }).ToList(),
            totalCount,
            statusFilter);
    }

    public async Task<PrivateMessageReportReviewContext?> GetReportedMessageContextAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PrivateMessageReports
            .AsNoTracking()
            .SingleOrDefaultAsync(report => report.Id == reportId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var memberIds = new[] { entity.ReporterMemberId, entity.ReportedMemberId };
        var names = await dbContext.MemberAccounts
            .AsNoTracking()
            .Where(member => memberIds.Contains(member.Id))
            .Select(member => new { member.Id, member.DisplayName })
            .ToListAsync(cancellationToken);
        var nameById = names.ToDictionary(row => row.Id, row => row.DisplayName);

        var auditEntities = await dbContext.PrivateMessageReportAuditLogs
            .AsNoTracking()
            .Where(log => log.ReportId == reportId)
            .ToListAsync(cancellationToken);
        var auditLogs = auditEntities
            .OrderByDescending(log => log.OccurredAt)
            .ThenByDescending(log => log.Id)
            .Select(MapAudit)
            .ToList();

        return new PrivateMessageReportReviewContext(
            PrivateMessageReportMapping.ToModel(entity),
            DisplayNameOrUnknown(nameById.GetValueOrDefault(entity.ReporterMemberId)),
            DisplayNameOrUnknown(nameById.GetValueOrDefault(entity.ReportedMemberId)),
            entity.ReviewerEmail,
            entity.ReviewedAt,
            entity.ReviewNotes,
            auditLogs);
    }

    public async Task<PrivateMessageReport?> UpdateReportStatusAsync(
        Guid reportId,
        string status,
        string actorEmail,
        string? reviewNotes,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PrivateMessageReports
            .SingleOrDefaultAsync(report => report.Id == reportId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var normalized = PrivateMessageReportStatus.Normalize(status);
        var previous = entity.Status;
        entity.Status = normalized;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.ReviewerEmail = NormalizeOptional(actorEmail, 256);
        entity.ReviewNotes = NormalizeOptional(reviewNotes, PrivateMessageLimits.MaxReportReviewNotesLength);

        dbContext.PrivateMessageReportAuditLogs.Add(new PrivateMessageReportAuditLogEntity
        {
            ReportId = reportId,
            Action = PrivateMessageReportAuditActions.StatusChanged,
            ActorEmail = NormalizeRequired(actorEmail, 256),
            OccurredAt = DateTimeOffset.UtcNow,
            Details = $"{previous} → {normalized}",
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return PrivateMessageReportMapping.ToModel(entity);
    }

    public async Task<bool> RecordAccessAsync(
        Guid reportId,
        string action,
        string actorEmail,
        string? details,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.PrivateMessageReports
            .AsNoTracking()
            .AnyAsync(report => report.Id == reportId, cancellationToken);
        if (!exists)
        {
            return false;
        }

        dbContext.PrivateMessageReportAuditLogs.Add(new PrivateMessageReportAuditLogEntity
        {
            ReportId = reportId,
            Action = action.Trim(),
            ActorEmail = NormalizeRequired(actorEmail, 256),
            OccurredAt = DateTimeOffset.UtcNow,
            Details = NormalizeOptional(details, 2000),
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<int> CountOpenAsync(CancellationToken cancellationToken = default) =>
        dbContext.PrivateMessageReports
            .AsNoTracking()
            .CountAsync(report => report.Status == PrivateMessageReportStatus.Open, cancellationToken);

    private bool IsSqliteDatabase() =>
        string.Equals(
            dbContext.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.Sqlite",
            StringComparison.Ordinal);

    private static string? NormalizeOptionalStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return PrivateMessageReportStatus.Normalize(status);
    }

    private static string DisplayNameOrUnknown(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "Unknown member" : name.Trim();

    private static string NormalizeRequired(string value, int maxLength)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static PrivateMessageReportAuditLog MapAudit(PrivateMessageReportAuditLogEntity entity) =>
        new(
            entity.Id,
            entity.ReportId,
            entity.Action,
            entity.ActorEmail,
            entity.OccurredAt,
            entity.Details);
}
