using QueenZone.Web.Pages.Admin.NewsDiscovery;

namespace QueenZone.Web.Tests;

public sealed class NewsAgentGuidanceDiffTests
{
    [Fact]
    public void Compare_marks_added_removed_and_changed_lines()
    {
        var diff = NewsAgentGuidanceDiff.Compare("keep short\nold line", "keep short\nnew line\nextra");

        Assert.Equal(NewsAgentGuidanceDiffKind.Unchanged, diff[0].Kind);
        Assert.Equal(NewsAgentGuidanceDiffKind.Changed, diff[1].Kind);
        Assert.Equal(NewsAgentGuidanceDiffKind.Added, diff[2].Kind);
    }
}
