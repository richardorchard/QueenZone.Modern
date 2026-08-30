using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.TriviaSubmissions;

public sealed class DetailModel(ITriviaFactSubmissionRepository triviaFactSubmissionRepository)
    : AdminTriviaSubmissionsPageModel
{
    public TriviaFactSubmission? Submission { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Submission = await triviaFactSubmissionRepository.GetByIdAsync(id, cancellationToken);
        if (Submission is null)
        {
            return NotFound();
        }

        StatusMessage = TempData["TriviaSubmissionMessage"] as string;
        StatusMessageKind = TempData["TriviaSubmissionMessageKind"] as string;
        ViewData["Title"] = "Review trivia suggestion";
        return Page();
    }
}
