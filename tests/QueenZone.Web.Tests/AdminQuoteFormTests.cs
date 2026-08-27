using QueenZone.Data;
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

    [Fact]
    public void NewModel_BuildForm_creates_add_quote_view_model()
    {
        var draft = new AdminQuoteDraft("Text", "Speaker", true);

        var form = NewModel.BuildForm(draft, ["Quote text is required."]);

        Assert.Equal("Add quote", form.Title);
        Assert.Equal("/admin/quotes", form.Action);
        Assert.Same(draft, form.Draft);
        Assert.Equal("Quote text is required.", Assert.Single(form.Errors!));
        Assert.Null(form.Quote);
    }

    [Fact]
    public void EditModel_BuildForm_creates_edit_quote_view_model()
    {
        var quote = new QuoteItem(7, "A kind of magic.", "Freddie Mercury", DateTime.UtcNow, true);
        var draft = new AdminQuoteDraft(quote.Text, quote.WhoSaid, quote.IsPublished);

        var form = EditModel.BuildForm(quote, draft, null);

        Assert.Equal("Edit quote", form.Title);
        Assert.Equal("/admin/quotes/7", form.Action);
        Assert.Same(draft, form.Draft);
        Assert.Null(form.Errors);
        Assert.Equal(7, form.Quote!.Id);
    }
}
