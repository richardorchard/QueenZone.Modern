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

    /// <summary>
    /// Optional 1-based page. When omitted, the latest page (newest messages) is shown.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? PageNumber { get; set; }

    public PrivateConversationDetail? Detail { get; private set; }

    public ArchivePaginationViewModel? Pagination { get; private set; }

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
            page: PageNumber,
            cancellationToken: cancellationToken);
        if (Detail is null)
        {
            return NotFound();
        }

        PageNumber = Detail.Page;
        Pagination = BuildPagination(Detail);
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
                page: PageNumber,
                cancellationToken: cancellationToken);
            if (Detail is null)
            {
                return NotFound();
            }

            PageNumber = Detail.Page;
            Pagination = BuildPagination(Detail);
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
                page: PageNumber,
                cancellationToken: cancellationToken);
            if (Detail is null)
            {
                return NotFound();
            }

            PageNumber = Detail.Page;
            Pagination = BuildPagination(Detail);
            ViewData["Title"] = $"Message {Detail.OtherParticipantDisplayName}";
            return Page();
        }

        // Redirect to the latest page so the new reply is visible.
        return RedirectToPage("./Conversation", new { conversationId = ConversationId });
    }

    public async Task<IActionResult> OnPostArchiveAsync(CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Challenge();
        }

        var archived = await privateMessageService.ArchiveConversationAsync(
            ConversationId,
            memberId.Value,
            cancellationToken);
        if (!archived)
        {
            return NotFound();
        }

        TempData[IndexModel.SuccessMessageKey] = "Conversation archived.";
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostRemoveAsync(CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Challenge();
        }

        var removed = await privateMessageService.RemoveConversationAsync(
            ConversationId,
            memberId.Value,
            cancellationToken);
        if (!removed)
        {
            return NotFound();
        }

        TempData[IndexModel.SuccessMessageKey] = "Conversation removed from your inbox.";
        return RedirectToPage("./Index");
    }

    private ArchivePaginationViewModel? BuildPagination(PrivateConversationDetail detail) =>
        ArchivePagination.BuildViewModel(
            "Conversation message pagination",
            detail.Page,
            detail.TotalPages,
            page => page >= detail.TotalPages
                ? $"/messages/{ConversationId}"
                : $"/messages/{ConversationId}?pageNumber={page}");

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
