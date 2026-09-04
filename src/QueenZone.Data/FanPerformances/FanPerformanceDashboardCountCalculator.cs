namespace QueenZone.Data;

internal static class FanPerformanceDashboardCountCalculator
{
    public static int NormalizeStaleAfterDays(int staleAfterDays) =>
        staleAfterDays < 1 ? FanPerformanceDashboardCounts.DefaultStaleAfterDays : staleAfterDays;

    public static int? ToOldestOpenAgeDays(DateTimeOffset utcNow, DateTimeOffset? oldestOpenSubmittedAt)
    {
        if (oldestOpenSubmittedAt is null)
        {
            return null;
        }

        return Math.Max(0, (int)Math.Floor((utcNow - oldestOpenSubmittedAt.Value).TotalDays));
    }

    public static FanPerformanceDashboardCounts FromRows(
        IReadOnlyList<(string Status, DateTimeOffset SubmittedAt)> rows,
        DateTimeOffset utcNow,
        int staleAfterDays)
    {
        staleAfterDays = NormalizeStaleAfterDays(staleAfterDays);
        var today = utcNow.UtcDateTime.Date;
        var weekAgo = today.AddDays(-6);
        var monthAgo = utcNow.AddDays(-30);
        var staleCutoff = utcNow.AddDays(-staleAfterDays);

        var pending = 0;
        var receivedToday = 0;
        var receivedThisWeek = 0;
        var approvedLast30 = 0;
        var rejectedLast30 = 0;
        var pendingLast30 = 0;
        var stalePending = 0;
        DateTimeOffset? oldestOpen = null;

        foreach (var row in rows)
        {
            var open = FanPerformanceSubmissionWorkflow.CanAdminAct(row.Status);
            if (open)
            {
                pending++;
                if (oldestOpen is null || row.SubmittedAt < oldestOpen.Value)
                {
                    oldestOpen = row.SubmittedAt;
                }

                if (row.SubmittedAt <= staleCutoff)
                {
                    stalePending++;
                }
            }

            if (row.SubmittedAt.UtcDateTime.Date >= today)
            {
                receivedToday++;
            }

            if (row.SubmittedAt.UtcDateTime.Date >= weekAgo)
            {
                receivedThisWeek++;
            }

            if (row.SubmittedAt < monthAgo)
            {
                continue;
            }

            if (row.Status == FanPerformanceSubmissionStatus.Approved)
            {
                approvedLast30++;
            }
            else if (row.Status == FanPerformanceSubmissionStatus.Rejected)
            {
                rejectedLast30++;
            }
            else if (open)
            {
                pendingLast30++;
            }
        }

        return new FanPerformanceDashboardCounts(
            new SubmissionTypeCounts(
                pending,
                receivedToday,
                receivedThisWeek,
                approvedLast30,
                rejectedLast30,
                pendingLast30),
            stalePending,
            ToOldestOpenAgeDays(utcNow, oldestOpen));
    }
}
