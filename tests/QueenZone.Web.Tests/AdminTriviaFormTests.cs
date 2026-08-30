using QueenZone.Data;
using QueenZone.Web.Pages.Admin.Trivia;

namespace QueenZone.Web.Tests;

public sealed class AdminTriviaFormTests
{
    [Fact]
    public void Defaults_are_empty_text_and_unpublished()
    {
        var form = new AdminTriviaForm();

        Assert.Equal(string.Empty, form.Text);
        Assert.Null(form.Category);
        Assert.Null(form.Difficulty);
        Assert.Null(form.Source);
        Assert.False(form.IsPublished);
    }

    [Fact]
    public void ToDraft_trims_fields_and_normalizes_difficulty()
    {
        var form = new AdminTriviaForm
        {
            Text = "  Freddie was born in Zanzibar.  ",
            Category = "  Band  ",
            Difficulty = "  Easy  ",
            Source = "  Biography  ",
            IsPublished = true,
        };

        var draft = form.ToDraft();

        Assert.Equal("Freddie was born in Zanzibar.", draft.Text);
        Assert.Equal("Band", draft.Category);
        Assert.Equal(TriviaDifficulty.Easy, draft.Difficulty);
        Assert.Equal("Biography", draft.Source);
        Assert.True(draft.IsPublished);
    }

    [Fact]
    public void ToDraft_treats_blank_optional_fields_as_null()
    {
        var form = new AdminTriviaForm
        {
            Text = null!,
            Category = "   ",
            Difficulty = string.Empty,
            Source = "  ",
            IsPublished = false,
        };

        var draft = form.ToDraft();

        Assert.Equal(string.Empty, draft.Text);
        Assert.Null(draft.Category);
        Assert.Null(draft.Difficulty);
        Assert.Null(draft.Source);
        Assert.False(draft.IsPublished);
    }

    [Fact]
    public void NewModel_BuildForm_creates_add_trivia_view_model()
    {
        var draft = new AdminTriviaDraft("Text", true, "Band", TriviaDifficulty.Easy, null);

        var form = NewModel.BuildForm(draft, ["Fact text is required."]);

        Assert.Equal("Add trivia fact", form.Title);
        Assert.Equal("/admin/trivia", form.Action);
        Assert.Same(draft, form.Draft);
        Assert.Equal("Fact text is required.", Assert.Single(form.Errors!));
        Assert.Null(form.Fact);
    }

    [Fact]
    public void EditModel_BuildForm_creates_edit_trivia_view_model()
    {
        var fact = new TriviaFactItem(7, "A kind of magic.", DateTime.UtcNow, true, "Albums", TriviaDifficulty.Medium);
        var draft = new AdminTriviaDraft(fact.Text, fact.IsPublished, fact.Category, fact.Difficulty);

        var form = EditModel.BuildForm(fact, draft, null);

        Assert.Equal("Edit trivia fact", form.Title);
        Assert.Equal("/admin/trivia/7", form.Action);
        Assert.Same(draft, form.Draft);
        Assert.Null(form.Errors);
        Assert.Equal(7, form.Fact!.Id);
    }
}
