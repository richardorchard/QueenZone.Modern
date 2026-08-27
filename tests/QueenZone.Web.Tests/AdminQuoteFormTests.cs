using QueenZone.Web.Pages.Admin.Quotes;

namespace QueenZone.Web.Tests;

public sealed class AdminQuoteFormTests
{
    [Fact]
    public void Defaults_are_empty_text_and_unpublished()
    {
        var form = new AdminQuoteForm();

        Assert.Equal(string.Empty, form.Text);
        Assert.Equal(string.Empty, form.WhoSaid);
        Assert.False(form.IsPublished);
    }

    [Fact]
    public void ToDraft_trims_fields_and_keeps_publish_state()
    {
        var form = new AdminQuoteForm
        {
            Text = "  A kind of magic.  ",
            WhoSaid = "  Freddie Mercury  ",
            IsPublished = true,
        };

        var draft = form.ToDraft();

        Assert.Equal("A kind of magic.", draft.Text);
        Assert.Equal("Freddie Mercury", draft.WhoSaid);
        Assert.True(draft.IsPublished);
    }

    [Fact]
    public void ToDraft_treats_null_fields_as_empty()
    {
        var form = new AdminQuoteForm
        {
            Text = null!,
            WhoSaid = null!,
            IsPublished = false,
        };

        var draft = form.ToDraft();

        Assert.Equal(string.Empty, draft.Text);
        Assert.Equal(string.Empty, draft.WhoSaid);
        Assert.False(draft.IsPublished);
    }
}
