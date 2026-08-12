using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed record MemberStats(int Total, int NewToday, int NewLast7Days, int NewLast30Days);

public sealed record RecentLogin(Guid MemberId, string DisplayName, bool HasAvatar, DateTime LastLoginAt);

public sealed record DailyRegistration(DateOnly Date, int Count);

public sealed record MemberSearchResult(IReadOnlyList<MemberAccount> Members, int TotalCount);
