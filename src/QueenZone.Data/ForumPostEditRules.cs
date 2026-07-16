namespace QueenZone.Data;

public static class ForumPostEditRules
{
    public const int EditGracePeriodMinutes = 5;

    public static bool CanEdit(
        Guid? authorMemberId,
        Guid? currentMemberId,
        bool isAdmin,
        DateTimeOffset postedAt,
        int editWindowMinutes,
        DateTimeOffset utcNow)
    {
        if (isAdmin)
        {
            return true;
        }

        if (editWindowMinutes == 0)
        {
            return false;
        }

        if (authorMemberId is null
            || currentMemberId is null
            || authorMemberId.Value != currentMemberId.Value)
        {
            return false;
        }

        if (editWindowMinutes < 0)
        {
            return true;
        }

        return utcNow <= postedAt.AddMinutes(editWindowMinutes);
    }

    public static bool ShowEditedIndicator(
        int editCount,
        DateTimeOffset? editedAt,
        DateTimeOffset postedAt)
    {
        if (editCount <= 0 || editedAt is null)
        {
            return false;
        }

        if (editCount == 1 && editedAt.Value <= postedAt.AddMinutes(EditGracePeriodMinutes))
        {
            return false;
        }

        return true;
    }

    public static string FormatEditedLabel(int editCount, DateTimeOffset editedAt, DateTimeOffset utcNow)
    {
        var relative = FormatRelativeTime(utcNow - editedAt);
        if (editCount <= 1)
        {
            return $"Edited {relative} ago";
        }

        return $"Edited {editCount} times · last edit {relative} ago";
    }

    public static string FormatRelativeTime(TimeSpan age)
    {
        if (age < TimeSpan.Zero)
        {
            age = TimeSpan.Zero;
        }

        if (age.TotalMinutes < 1)
        {
            return "moments";
        }

        if (age.TotalMinutes < 60)
        {
            var minutes = Math.Max(1, (int)age.TotalMinutes);
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }

        if (age.TotalHours < 24)
        {
            var hours = Math.Max(1, (int)age.TotalHours);
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }

        if (age.TotalDays < 30)
        {
            var days = Math.Max(1, (int)age.TotalDays);
            return days == 1 ? "1 day" : $"{days} days";
        }

        var months = Math.Max(1, (int)(age.TotalDays / 30));
        return months == 1 ? "1 month" : $"{months} months";
    }
}
