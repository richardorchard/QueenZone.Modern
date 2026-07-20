using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.NewsSuggestions;

public sealed class DetailModel(
    INewsSuggestionRepository newsSuggestionRepository,
    INewsDiscoveryRepository newsDiscoveryRepository) : AdminNewsSuggestionsPageModel
{
    public NewsSuggestion? Suggestion { get; private set; }

    public NewsCandidate? MatchingCandidate { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Suggestion = await newsSuggestionRepository.GetByIdAsync(id, cancellationToken);
        if (Suggestion is null)
        {
            return NotFound();
        }

        MatchingCandidate = await newsDiscoveryRepository.GetCandidateByCanonicalUrlHashAsync(
            Suggestion.UrlHash,
            cancellationToken);

        StatusMessage = TempData["NewsSuggestionMessage"] as string;
        StatusMessageKind = TempData["NewsSuggestionMessageKind"] as string;
        ViewData["Title"] = $"Review suggestion — {Suggestion.Title ?? Suggestion.Url}";
        return Page();
    }
}
