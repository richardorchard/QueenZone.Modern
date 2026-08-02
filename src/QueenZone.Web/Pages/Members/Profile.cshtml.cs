using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Pages.Members;

public sealed class ProfileModel(IMemberAccountRepository memberAccountRepository) : PageModel
{
    public MemberAccount? Member { get; private set; }

    public Guid? CurrentMemberId { get; private set; }

    public bool CanMessage { get; private set; }

    public bool IsSignedIn { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid memberId, CancellationToken cancellationToken)
    {
        Member = await memberAccountRepository.FindByIdAsync(memberId, cancellationToken);
        if (Member is null)
        {
            return NotFound();
        }

        CurrentMemberId = await GetCurrentMemberIdAsync();
        IsSignedIn = CurrentMemberId is not null;
        CanMessage = PrivateMessageService.CanMessage(CurrentMemberId, Member.Id);
        ViewData["Title"] = Member.DisplayName;
        return Page();
    }

    private async Task<Guid?> GetCurrentMemberIdAsync()
    {
        var directId = ForumMember.GetMemberId(User);
        if (directId is not null)
        {
            return directId;
        }

        var memberCookie = await HttpContext.AuthenticateAsync(MemberAuthenticationSchemes.MembersCookie);
        if (memberCookie.Succeeded)
        {
            return ForumMember.GetMemberId(memberCookie.Principal);
        }

        if (HttpContext.RequestServices.GetService<IHostEnvironment>()?.IsEnvironment("Testing") == true)
        {
            var testMember = await HttpContext.AuthenticateAsync(TestMemberAuthHandler.SchemeName);
            if (testMember.Succeeded)
            {
                return ForumMember.GetMemberId(testMember.Principal);
            }
        }

        return null;
    }
}
