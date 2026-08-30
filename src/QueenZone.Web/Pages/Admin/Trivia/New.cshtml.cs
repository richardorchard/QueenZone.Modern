using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Trivia;

public sealed class NewModel : AdminTriviaPageModel
{
    public TriviaFormViewModel Form { get; private set; } = BuildForm(
        new AdminTriviaDraft(string.Empty, true, null, null, null),
        null);

    public void OnGet()
    {
        ViewData["Title"] = "Add trivia fact";
    }

    public static TriviaFormViewModel BuildForm(AdminTriviaDraft draft, IReadOnlyList<string>? errors) =>
        new("Add trivia fact", "/admin/trivia", draft, errors);
}
