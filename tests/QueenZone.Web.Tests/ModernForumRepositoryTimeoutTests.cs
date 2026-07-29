using QueenZone.Data;

namespace QueenZone.Web.Tests;

/// <summary>
/// Guards the interactive vs sitemap timeout split on modern forum reads (#402).
/// Values are mirrored from <see cref="ModernForumRepository"/> private constants.
/// </summary>
public sealed class ModernForumRepositoryTimeoutTests
{
    [Fact]
    public void Interactive_forum_timeout_matches_public_default()
    {
        Assert.Equal(30, QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds);
        // ModernForumRepository.InteractiveCommandTimeoutSeconds uses DefaultCommandTimeoutSeconds.
        Assert.Equal(
            QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds,
            GetPrivateConstInt(typeof(ModernForumRepository), "InteractiveCommandTimeoutSeconds"));
    }

    [Fact]
    public void Sitemap_forum_timeout_remains_elevated()
    {
        Assert.Equal(120, GetPrivateConstInt(typeof(ModernForumRepository), "SitemapCommandTimeoutSeconds"));
    }

    private static int GetPrivateConstInt(Type type, string name)
    {
        var field = type.GetField(
            name,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(field);
        return Assert.IsType<int>(field.GetRawConstantValue());
    }
}
