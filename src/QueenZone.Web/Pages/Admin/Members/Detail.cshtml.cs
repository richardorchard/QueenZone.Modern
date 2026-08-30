using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Pages.Admin.Members;

public sealed class DetailModel(
    IMemberAccountRepository memberAccountRepository,
    IForumWriteRepository forumWriteRepository) : AdminMembersPageModel
{
    public MemberAccount? Member { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public ForumAuthorContentSummary? ForumContent { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Member = await memberAccountRepository.FindByIdAsync(id, cancellationToken);
        if (Member is null)
        {
            return NotFound();
        }

        ForumContent = await forumWriteRepository.GetAuthorForumContentSummaryAsync(
            Member.Id, Member.DisplayName, cancellationToken);

        StatusMessage = TempData["MemberMessage"] as string;
        StatusMessageKind = TempData["MemberMessageKind"] as string;
        ViewData["Title"] = $"Member — {Member.DisplayName}";
        return Page();
    }
}
