using Microsoft.AspNetCore.Http;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Shared option parsing and vote/close error mapping for the website form
/// endpoints and <c>/api/v1/forum</c> poll routes. Repository rules stay in
/// <see cref="IForumPollRepository"/>; this type only translates them to HTTP.
/// </summary>
public static class ForumPollVoteMapper
{
    public static Guid[] ParseOptionIds(IFormCollection form) =>
        ParseOptionIds(form["optionIds"], form["optionId"]);

    public static Guid[] ParseOptionIds(
        IEnumerable<string?> optionIds,
        IEnumerable<string?> optionId) =>
        optionIds
            .SelectMany(value => (value ?? string.Empty).Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Concat(optionId.Where(value => !string.IsNullOrWhiteSpace(value)))
            .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

    public static Guid[] ParseOptionIds(IReadOnlyList<Guid>? optionIds, Guid? optionId)
    {
        var ids = new List<Guid>();
        if (optionIds is not null)
        {
            ids.AddRange(optionIds);
        }

        if (optionId is Guid single && single != Guid.Empty)
        {
            ids.Add(single);
        }

        return ids.Where(id => id != Guid.Empty).Distinct().ToArray();
    }

    public static int StatusCode(ForumPollVoteException ex) =>
        ex.Code switch
        {
            ForumPollVoteException.AlreadyVoted => StatusCodes.Status409Conflict,
            ForumPollVoteException.NotFound => StatusCodes.Status404NotFound,
            ForumPollVoteException.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };

    public static string Title(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status403Forbidden => "Forbidden",
            _ => "Bad Request",
        };

    /// <summary>Website form endpoints keep the existing <c>{ error, code }</c> JSON.</summary>
    public static IResult ToFormResult(ForumPollVoteException ex)
    {
        var status = StatusCode(ex);
        var payload = new { error = ex.Message, code = ex.Code };
        return status switch
        {
            StatusCodes.Status409Conflict => Results.Conflict(payload),
            StatusCodes.Status404NotFound => Results.NotFound(payload),
            StatusCodes.Status403Forbidden => Results.Forbid(),
            _ => Results.BadRequest(payload),
        };
    }

    /// <summary>JSON API uses Problem Details plus the same <c>code</c> as the website.</summary>
    public static IResult ToProblemResult(ForumPollVoteException ex)
    {
        var status = StatusCode(ex);
        return Results.Problem(
            statusCode: status,
            title: Title(status),
            detail: ex.Message,
            extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
    }
}
