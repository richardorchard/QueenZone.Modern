using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Polls;

public sealed class NewModel : AdminPollPageModel
{
    public PollFormViewModel Form { get; private set; } = BuildForm(
        new AdminHomePollDraft(string.Empty, ["", ""]),
        null);

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Add poll";
        Breadcrumbs = AdminBreadcrumbs.Page("Home polls", "/admin/polls", "Add poll");
    }

    public static PollFormViewModel BuildForm(AdminHomePollDraft draft, IReadOnlyList<string>? errors) =>
        new("Add poll", "/admin/polls", draft, errors);
}
