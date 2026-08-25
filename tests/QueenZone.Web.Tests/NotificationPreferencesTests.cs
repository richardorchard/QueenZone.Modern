using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class NotificationPreferencesTests
{
    [Fact]
    public void Defaults_AreForumAndMessageOn_NewsOff()
    {
        var defaults = NotificationPreferences.Defaults;

        Assert.True(defaults.ForumReply);
        Assert.True(defaults.PrivateMessage);
        Assert.False(defaults.News);
        Assert.True(defaults.IsEnabled(NotificationCategory.ForumReply));
        Assert.True(defaults.IsEnabled(NotificationCategory.PrivateMessage));
        Assert.False(defaults.IsEnabled(NotificationCategory.News));
    }

    [Fact]
    public void IsEnabled_Throws_ForUnknownCategory()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => NotificationPreferences.Defaults.IsEnabled((NotificationCategory)99));
    }

    [Fact]
    public void Resolve_EmptyChoices_ReturnsDefaults()
    {
        var resolved = NotificationPreferencesMerge.Resolve([]);

        Assert.Equal(NotificationPreferences.Defaults, resolved);
    }

    [Fact]
    public void Resolve_OverlaysKnownCategories()
    {
        var resolved = NotificationPreferencesMerge.Resolve(
        [
            (NotificationCategory.ForumReply, false),
            (NotificationCategory.News, true),
        ]);

        Assert.False(resolved.ForumReply);
        Assert.True(resolved.PrivateMessage);
        Assert.True(resolved.News);
    }

    [Fact]
    public void Resolve_IgnoresUnknownCategory()
    {
        var resolved = NotificationPreferencesMerge.Resolve(
        [
            ((NotificationCategory)99, true),
            (NotificationCategory.News, true),
        ]);

        Assert.Equal(new NotificationPreferences(true, true, true), resolved);
    }

    [Fact]
    public void Resolve_LastChoiceWins_ForSameCategory()
    {
        var resolved = NotificationPreferencesMerge.Resolve(
        [
            (NotificationCategory.News, true),
            (NotificationCategory.News, false),
        ]);

        Assert.False(resolved.News);
    }

    [Fact]
    public void Apply_ReplacesOnlySuppliedFields()
    {
        var current = NotificationPreferences.Defaults;
        var patched = NotificationPreferencesMerge.Apply(current, new NotificationPreferencePatch(false, null, true));

        Assert.False(patched.ForumReply);
        Assert.True(patched.PrivateMessage);
        Assert.True(patched.News);
    }

    [Fact]
    public void Apply_EmptyPatch_IsIdentity()
    {
        var current = new NotificationPreferences(false, false, true);

        Assert.Equal(current, NotificationPreferencesMerge.Apply(current, new NotificationPreferencePatch(null, null, null)));
    }

    [Fact]
    public void Apply_IsIdempotent()
    {
        var patch = new NotificationPreferencePatch(null, null, true);
        var once = NotificationPreferencesMerge.Apply(NotificationPreferences.Defaults, patch);
        var twice = NotificationPreferencesMerge.Apply(once, patch);

        Assert.Equal(once, twice);
        Assert.True(once.News);
    }

    [Fact]
    public void Patch_IsEmpty_WhenAllFieldsNull()
    {
        Assert.True(new NotificationPreferencePatch(null, null, null).IsEmpty);
        Assert.False(new NotificationPreferencePatch(false, null, null).IsEmpty);
    }
}
