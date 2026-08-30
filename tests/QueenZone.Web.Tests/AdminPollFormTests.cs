using QueenZone.Data;
using QueenZone.Web.Pages.Admin.Polls;

namespace QueenZone.Web.Tests;

public sealed class AdminPollFormTests
{
    [Fact]
    public void ToDraft_trims_question_and_drops_blank_options()
    {
        var form = new AdminPollForm
        {
            Question = "  Best album?  ",
            OptionTexts = ["  Opera  ", "  ", "News of the World"],
        };

        var draft = form.ToDraft();

        Assert.Equal("Best album?", draft.Question);
        Assert.Equal(["Opera", "News of the World"], draft.Options);
    }

    [Fact]
    public void NewModel_BuildForm_posts_to_list()
    {
        var form = NewModel.BuildForm(new AdminHomePollDraft("Q", ["A", "B"]), null);

        Assert.Equal("Add poll", form.Title);
        Assert.Equal("/admin/polls", form.Action);
        Assert.False(form.OptionsLocked);
    }

    [Fact]
    public void EditModel_BuildForm_locks_options_after_votes()
    {
        var poll = new HomePollAdminDetail(
            Guid.NewGuid(),
            "Q",
            true,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            VoteCount: 2,
            [new HomePollOptionResult(Guid.NewGuid(), "A", 0, 2, 100)]);

        var form = EditModel.BuildForm(poll, EditModel.ToDraft(poll), null);

        Assert.Equal($"/admin/polls/{poll.Id}", form.Action);
        Assert.True(form.OptionsLocked);
        Assert.Equal(["A"], form.Draft.Options);
    }
}
