using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Messages;

[Authorize(Policy = MemberAuthenticationSchemes.MemberPolicy)]
public sealed class IndexModel(PrivateMessageService privateMessageService) : PageModel
{
    public IReadOnlyList<PrivateConversationListItem> Conversations { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Challenge();
        }

        Conversations = await privateMessageService.GetInboxAsync(memberId.Value, cancellationToken);
        ViewData["Title"] = "Messages";
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
