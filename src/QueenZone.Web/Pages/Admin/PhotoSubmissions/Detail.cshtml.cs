using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.PhotoSubmissions;

public sealed class DetailModel(
    IPhotoSubmissionRepository photoSubmissionRepository,
    IPhotoRepository photoRepository) : AdminPhotoSubmissionsPageModel
{
    public PhotoSubmission? Submission { get; private set; }

    public IReadOnlyList<PhotoCategory> Categories { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Submission = await photoSubmissionRepository.GetByIdAsync(id, cancellationToken);
        if (Submission is null)
        {
            return NotFound();
        }

        Categories = await photoRepository.GetCategoriesAsync(cancellationToken);
        StatusMessage = TempData["PhotoSubmissionMessage"] as string;
        StatusMessageKind = TempData["PhotoSubmissionMessageKind"] as string;
        ViewData["Title"] = $"Review photo — {Submission.Title}";
        return Page();
    }
}
