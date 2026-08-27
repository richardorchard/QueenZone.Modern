using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Members;

public sealed class ActionModel(
    IMemberAccountRepository memberAccountRepository,
    IForumWriteRepository forumWriteRepository,
    IMobileAuthGrantRepository mobileAuthGrantRepository) : AdminMembersPageModel
{
    [BindProperty]
    public string? Reason { get; set; }

    public async Task<IActionResult> OnPostSuspendAsync(Guid id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Reason))
        {
            return RedirectWithMessage(id, "A reason is required to suspend a member.", "error");
        }

        var updated = await memberAccountRepository.SuspendAsync(
            id, Reason.Trim(), EditorEmail, DateTime.UtcNow, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        await forumWriteRepository.HidePostsByMemberAsync(id, cancellationToken);
        await mobileAuthGrantRepository.RevokeAllRefreshTokensForMemberAsync(id, DateTime.UtcNow, cancellationToken);

        return RedirectWithMessage(
            id,
            "Member suspended and their forum topics and posts hidden. Their session will end on their next request.",
            "success");
    }

    public async Task<IActionResult> OnPostReinstateAsync(Guid id, CancellationToken cancellationToken)
    {
        var updated = await memberAccountRepository.ReinstateAsync(id, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        await forumWriteRepository.UnhidePostsByMemberAsync(id, cancellationToken);

        return RedirectWithMessage(id, "Member reinstated and their forum posts restored.", "success");
    }

    private IActionResult RedirectWithMessage(Guid id, string message, string kind)
    {
        TempData["MemberMessage"] = message;
        TempData["MemberMessageKind"] = kind;
        return Redirect($"/admin/members/{id}");
    }
}
