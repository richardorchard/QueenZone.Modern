using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web;

public static class ForumPollEndpoints
{
    public static void MapForumPollEndpoints(this WebApplication app)
    {
        app.MapPost("/forum/poll/{pollId:guid}/vote", async (
                Guid pollId,
                HttpContext httpContext,
                IForumPollRepository pollRepository,
                IAntiforgery antiforgery,
                CancellationToken cancellationToken) =>
            await VoteAsync(pollId, httpContext, pollRepository, antiforgery, cancellationToken))
            .RequireAuthorization(MemberAuthenticationSchemes.MemberPolicy)
            .DisableAntiforgery()
            .WithName("VoteForumPoll");

        app.MapPost("/forum/poll/{pollId:guid}/close", async (
                Guid pollId,
                HttpContext httpContext,
                IForumPollRepository pollRepository,
                IAntiforgery antiforgery,
                IOptions<AdminOptions> adminOptions,
                CancellationToken cancellationToken) =>
            await CloseAsync(pollId, httpContext, pollRepository, antiforgery, adminOptions.Value, cancellationToken))
            .RequireAuthorization(MemberAuthenticationSchemes.MemberPolicy)
            .DisableAntiforgery()
            .WithName("CloseForumPoll");
    }

    internal static async Task<IResult> VoteAsync(
        Guid pollId,
        HttpContext httpContext,
        IForumPollRepository pollRepository,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        var memberId = ForumMember.GetMemberId(httpContext.User);
        if (memberId is null)
        {
            return Results.Unauthorized();
        }

        IFormCollection form;
        try
        {
            // Read form first so antiforgery can see __RequestVerificationToken.
            form = await httpContext.Request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            return Results.BadRequest(new { error = "Invalid form data." });
        }

        if (!await antiforgery.IsRequestValidAsync(httpContext))
        {
            return Results.BadRequest(new { error = "Invalid antiforgery token." });
        }

        var optionIds = form["optionIds"]
            .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Concat(form["optionId"].Where(value => !string.IsNullOrWhiteSpace(value)))
            .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        var returnUrl = form["returnUrl"].ToString();
        if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/forum/", StringComparison.Ordinal))
        {
            returnUrl = "/forum";
        }

        try
        {
            await pollRepository.CastVoteAsync(pollId, memberId.Value, optionIds, cancellationToken);
            return Results.Redirect(returnUrl + "#poll");
        }
        catch (ForumPollVoteException ex) when (ex.Code is ForumPollVoteException.AlreadyVoted)
        {
            return Results.Conflict(new { error = ex.Message, code = ex.Code });
        }
        catch (ForumPollVoteException ex) when (ex.Code is ForumPollVoteException.NotFound)
        {
            return Results.NotFound(new { error = ex.Message, code = ex.Code });
        }
        catch (ForumPollVoteException ex)
        {
            return Results.BadRequest(new { error = ex.Message, code = ex.Code });
        }
    }

    internal static async Task<IResult> CloseAsync(
        Guid pollId,
        HttpContext httpContext,
        IForumPollRepository pollRepository,
        IAntiforgery antiforgery,
        AdminOptions adminOptions,
        CancellationToken cancellationToken)
    {
        var memberId = ForumMember.GetMemberId(httpContext.User);
        if (memberId is null)
        {
            return Results.Unauthorized();
        }

        IFormCollection form;
        try
        {
            form = await httpContext.Request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            return Results.BadRequest(new { error = "Invalid form data." });
        }

        if (!await antiforgery.IsRequestValidAsync(httpContext))
        {
            return Results.BadRequest(new { error = "Invalid antiforgery token." });
        }

        var returnUrl = form["returnUrl"].ToString();
        if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/forum/", StringComparison.Ordinal))
        {
            returnUrl = "/forum";
        }

        var isAdmin = IsAdmin(httpContext.User, adminOptions);

        try
        {
            await pollRepository.ClosePollAsync(pollId, memberId.Value, isAdmin, cancellationToken);
            return Results.Redirect(returnUrl + "#poll");
        }
        catch (ForumPollVoteException ex) when (ex.Code is ForumPollVoteException.Forbidden)
        {
            return Results.Forbid();
        }
        catch (ForumPollVoteException ex) when (ex.Code is ForumPollVoteException.NotFound)
        {
            return Results.NotFound(new { error = ex.Message, code = ex.Code });
        }
        catch (ForumPollVoteException ex)
        {
            return Results.BadRequest(new { error = ex.Message, code = ex.Code });
        }
    }

    internal static bool IsAdmin(ClaimsPrincipal user, AdminOptions adminOptions)
    {
        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("preferred_username")
            ?? user.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return adminOptions.AllowedEmails.Any(allowed =>
            string.Equals(allowed, email, StringComparison.OrdinalIgnoreCase));
    }
}
