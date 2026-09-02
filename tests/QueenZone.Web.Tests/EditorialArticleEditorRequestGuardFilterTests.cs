using System.IO;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using QueenZone.Web.Pages.Admin.Articles;

namespace QueenZone.Web.Tests;

public sealed class EditorialArticleEditorRequestGuardFilterTests
{
    [Fact]
    public void IsHandled_covers_size_and_antiforgery_exceptions()
    {
        Assert.True(EditorialArticleEditorRequestGuardFilter.IsHandled(new BadHttpRequestException("Failed to read the request form.")));
        Assert.True(EditorialArticleEditorRequestGuardFilter.IsHandled(new InvalidDataException("Form value length limit 4194304 exceeded.")));
        Assert.True(EditorialArticleEditorRequestGuardFilter.IsHandled(new AntiforgeryValidationException("The required antiforgery cookie is not present.")));
        Assert.False(EditorialArticleEditorRequestGuardFilter.IsHandled(new InvalidOperationException("other")));
    }

    [Fact]
    public void MessageFor_maps_size_and_antiforgery_to_in_page_copy()
    {
        Assert.Equal(
            EditorialArticleEditorRequestGuardFilter.SizeError,
            EditorialArticleEditorRequestGuardFilter.MessageFor(new BadHttpRequestException("Request body too large.")));
        Assert.Equal(
            EditorialArticleEditorRequestGuardFilter.SizeError,
            EditorialArticleEditorRequestGuardFilter.MessageFor(new InvalidDataException("Form value length limit 4194304 exceeded.")));
        Assert.Equal(
            EditorialArticleEditorRequestGuardFilter.AntiforgeryError,
            EditorialArticleEditorRequestGuardFilter.MessageFor(new AntiforgeryValidationException("The required antiforgery cookie is not present.")));
        Assert.Equal(
            EditorialArticleEditorRequestGuardFilter.BindError,
            EditorialArticleEditorRequestGuardFilter.MessageFor(new FormatException("bad date")));
    }
}
