using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Members;

public sealed class AuthorActionModel(IForumWriteRepository forumWriteRepository) : AdminMembersPageModel
{
    [BindProperty]
    public string? DisplayName { get; set; }

    public async Task<IActionResult> OnPostHideForumContentAsync(CancellationToken cancellationToken)
    {
        var name = DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return RedirectWithMessage("A display name is required.", "error", name);
        }

        await forumWriteRepository.HideAuthorForumContentAsync(null, name, cancellationToken);
        return RedirectWithMessage(
            $"Hidden all posts and threads started by {name}. Other people's posts stay.",
            "success",
            name);
    }

    public async Task<IActionResult> OnPostUnhideForumContentAsync(CancellationToken cancellationToken)
    {
        var name = DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return RedirectWithMessage("A display name is required.", "error", name);
        }

        await forumWriteRepository.UnhideAuthorForumContentAsync(null, name, cancellationToken);
        return RedirectWithMessage($"Restored posts and threads started by {name}.", "success", name);
    }

    private IActionResult RedirectWithMessage(string message, string kind, string? query)
    {
        TempData["MemberMessage"] = message;
        TempData["MemberMessageKind"] = kind;
        return Redirect(string.IsNullOrWhiteSpace(query)
            ? "/admin/members"
            : $"/admin/members?query={Uri.EscapeDataString(query)}");
    }
}
