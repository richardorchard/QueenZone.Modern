using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages.Submit;

[Authorize(Policy = MemberAuthenticationSchemes.MemberPolicy, AuthenticationSchemes = MemberAuthenticationSchemes.MembersCookie)]
public sealed class NewsModel(NewsSuggestionService newsSuggestionService) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "URL is required.")]
    [StringLength(2000, ErrorMessage = "URL must be 2000 characters or fewer.")]
    [Display(Name = "News story URL")]
    public string StoryUrl { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(300, ErrorMessage = "Suggested headline must be 300 characters or fewer.")]
    [Display(Name = "Suggested headline")]
    public string? Title { get; set; }

    [BindProperty]
    [StringLength(1000, ErrorMessage = "Notes must be 1000 characters or fewer.")]
    [Display(Name = "Notes for the editor")]
    public string? Notes { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (await GetCurrentMemberIdAsync() is null)
        {
            return Redirect("/account/login");
        }

        ViewData["Title"] = "Suggest news";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Redirect("/account/login");
        }

        ViewData["Title"] = "Suggest news";

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await newsSuggestionService.SubmitAsync(
            memberId.Value,
            StoryUrl,
            Title,
            Notes,
            cancellationToken);

        if (!result.Succeeded || result.Suggestion is null)
        {
            if (result.IsDuplicateActive)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? NewsSuggestionService.DuplicateActiveMessage);
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not submit suggestion.");
            }

            return Page();
        }

        return Redirect("/submit/news/confirmation");
    }

    private async Task<Guid?> GetCurrentMemberIdAsync()
    {
        var authResult = await HttpContext.AuthenticateAsync(MemberAuthenticationSchemes.MembersCookie);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return null;
        }

        var idValue = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var id) ? id : null;
    }
}
