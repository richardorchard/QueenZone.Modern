using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ForumPostEditRulesTests
{
    private static readonly Guid AuthorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset PostedAt = DateTimeOffset.Parse("2026-07-16T10:00:00Z");

    [Fact]
    public void CanEdit_AllowsOwnerWithinWindow()
    {
        Assert.True(ForumPostEditRules.CanEdit(
            AuthorId,
            AuthorId,
            isAdmin: false,
            PostedAt,
            editWindowMinutes: 60,
            PostedAt.AddMinutes(30)));
    }

    [Fact]
    public void CanEdit_RejectsOwnerAfterWindow()
    {
        Assert.False(ForumPostEditRules.CanEdit(
            AuthorId,
            AuthorId,
            isAdmin: false,
            PostedAt,
            editWindowMinutes: 60,
            PostedAt.AddMinutes(61)));
    }

    [Fact]
    public void CanEdit_RejectsNonOwner()
    {
        Assert.False(ForumPostEditRules.CanEdit(
            AuthorId,
            OtherId,
            isAdmin: false,
            PostedAt,
            editWindowMinutes: 60,
            PostedAt.AddMinutes(5)));
    }

    [Fact]
    public void CanEdit_AllowsAdminRegardlessOfAgeOrOwner()
    {
        Assert.True(ForumPostEditRules.CanEdit(
            AuthorId,
            OtherId,
            isAdmin: true,
            PostedAt,
            editWindowMinutes: 60,
            PostedAt.AddDays(30)));
    }

    [Fact]
    public void CanEdit_DisablesMemberEditingWhenWindowIsZero()
    {
        Assert.False(ForumPostEditRules.CanEdit(
            AuthorId,
            AuthorId,
            isAdmin: false,
            PostedAt,
            editWindowMinutes: 0,
            PostedAt.AddMinutes(1)));
        Assert.True(ForumPostEditRules.CanEdit(
            AuthorId,
            OtherId,
            isAdmin: true,
            PostedAt,
            editWindowMinutes: 0,
            PostedAt.AddMinutes(1)));
    }

    [Fact]
    public void CanEdit_AllowsUnlimitedWhenWindowIsNegative()
    {
        Assert.True(ForumPostEditRules.CanEdit(
            AuthorId,
            AuthorId,
            isAdmin: false,
            PostedAt,
            editWindowMinutes: -1,
            PostedAt.AddYears(1)));
    }

    [Fact]
    public void ShowEditedIndicator_HidesGracePeriodFirstEdit()
    {
        Assert.False(ForumPostEditRules.ShowEditedIndicator(
            editCount: 1,
            editedAt: PostedAt.AddMinutes(2),
            PostedAt));
    }

    [Fact]
    public void ShowEditedIndicator_ShowsAfterGraceOrMultipleEdits()
    {
        Assert.True(ForumPostEditRules.ShowEditedIndicator(
            editCount: 1,
            editedAt: PostedAt.AddMinutes(10),
            PostedAt));
        Assert.True(ForumPostEditRules.ShowEditedIndicator(
            editCount: 2,
            editedAt: PostedAt.AddMinutes(2),
            PostedAt));
    }

    [Fact]
    public void FormatEditedLabel_UsesRelativeTimeAndCount()
    {
        Assert.Equal(
            "Edited 12 minutes ago",
            ForumPostEditRules.FormatEditedLabel(1, PostedAt, PostedAt.AddMinutes(12)));
        Assert.Equal(
            "Edited 3 times · last edit 2 hours ago",
            ForumPostEditRules.FormatEditedLabel(3, PostedAt, PostedAt.AddHours(2)));
    }
}
