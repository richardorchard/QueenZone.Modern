using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.PhotoSubmissions;

public sealed class IndexModel(IPhotoSubmissionRepository photoSubmissionRepository) : AdminPhotoSubmissionsPageModel
{
    public IReadOnlyList<PhotoSubmissionListItem> Submissions { get; private set; } = [];

    public int PageNumber { get; private set; } = 1;

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task OnGetAsync(int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        PageNumber = Math.Max(1, pageNumber);
        Submissions = await photoSubmissionRepository.GetPendingAsync(PageNumber, 50, cancellationToken);
        ViewData["Title"] = "Photo submissions";
        Breadcrumbs = AdminBreadcrumbs.Section("Photo submissions", "/admin/photo-submissions");
    }
}
