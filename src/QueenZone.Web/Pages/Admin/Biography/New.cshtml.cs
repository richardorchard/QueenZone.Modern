using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Biography;

public sealed class NewModel(IBiographyRepository biographyRepository) : AdminBiographyPageModel
{
    public ChapterFormViewModel Form { get; private set; } = BuildForm(
        new AdminBiographyDraft(string.Empty, string.Empty, string.Empty, 0),
        null);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var chapters = await biographyRepository.GetChaptersAsync(cancellationToken);
        var nextSequence = chapters.Count == 0
            ? (byte)1
            : (byte)Math.Min(255, chapters.Max(chapter => chapter.DisplaySequence) + 1);
        Form = BuildForm(
            new AdminBiographyDraft(string.Empty, string.Empty, string.Empty, nextSequence),
            null);
        ViewData["Title"] = "Create biography chapter";
    }

    public static ChapterFormViewModel BuildForm(AdminBiographyDraft draft, IReadOnlyList<string>? errors) =>
        new("Create biography chapter", "/admin/biography", draft, errors);
}
