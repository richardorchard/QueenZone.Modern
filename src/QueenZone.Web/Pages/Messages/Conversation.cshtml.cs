using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Messages;

[Authorize(Policy = MemberAuthenticationSchemes.MemberPolicy)]
public sealed class ConversationModel(PrivateMessageService privateMessageService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid ConversationId { get; set; }

    public PrivateConversationDetail? Detail { get; private set; }

    [BindProperty]
    public ReplyInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Challenge();
        }

        Detail = await privateMessageService.GetConversationAsync(
            ConversationId,
            memberId.Value,
            markRead: true,
            cancellationToken);
        if (Detail is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Message {Detail.OtherParticipantDisplayName}";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            Detail = await privateMessageService.GetConversationAsync(
                ConversationId,
                memberId.Value,
                markRead: false,
                cancellationToken);
            if (Detail is null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Message {Detail.OtherParticipantDisplayName}";
            return Page();
        }

        var result = await privateMessageService.ReplyAsync(
            ConversationId,
            memberId.Value,
            Input.Body,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (string.Equals(
                    result.ErrorMessage,
                    "You are not a participant in this conversation.",
                    StringComparison.Ordinal))
            {
                return NotFound();
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to send reply.");
            Detail = await privateMessageService.GetConversationAsync(
                ConversationId,
                memberId.Value,
                markRead: false,
                cancellationToken);
            if (Detail is null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Message {Detail.OtherParticipantDisplayName}";
            return Page();
        }

        return RedirectToPage("./Conversation", new { conversationId = ConversationId });
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

    public sealed class ReplyInput
    {
        [Display(Name = "Reply")]
        [Required(ErrorMessage = "Message body is required.")]
        [StringLength(
            PrivateMessageLimits.MaxBodyLength,
            ErrorMessage = "Message body must be {1} characters or fewer.")]
        public string Body { get; set; } = string.Empty;
    }
}
