using System.Security.Claims;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Authenticated member writes under <c>/api/v1/member/*</c> (issue #746).
/// Photo submit requires <see cref="MemberAuthenticationSchemes.MobileMemberPolicy"/>
/// and delegates to <see cref="PhotoSubmissionService.SubmitAsync"/> — the same
/// <c>ugc-photos</c> path, admin queue, and <see cref="MemberUploadQuotaService"/>
/// bucket as <c>/submit/photo</c>. Do not add a second quota counter.
/// </summary>
public static class MemberApiEndpoints
{
    public const string RootPath = "/api/v1/member";

    public const string PhotoSubmissionsPath = $"{RootPath}/photo-submissions";

    public static void MapMemberApiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(RootPath)
            .WithGroupName(ApiV1.OpenApiDocumentName)
            .WithTags("Member")
            .DisableAntiforgery();

        group.MapPost("/photo-submissions", CreatePhotoSubmissionAsync)
            .WithName("CreateMemberPhotoSubmission")
            .WithSummary("Submit a photo for review. Same PhotoSubmissionService and daily quota as /submit/photo.")
            .RequireAuthorization(MemberAuthenticationSchemes.MobileMemberPolicy)
            .Accepts<PhotoSubmissionRequestDto>("multipart/form-data", "application/json")
            .Produces<PhotoSubmissionCreatedDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .DisableAntiforgery();
    }

    internal static async Task<IResult> CreatePhotoSubmissionAsync(
        ClaimsPrincipal user,
        HttpRequest request,
        PhotoSubmissionService photoSubmissionService,
        CancellationToken cancellationToken)
    {
        var memberId = ForumMember.GetMemberId(user);
        if (memberId is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");
        }

        if (!request.HasFormContentType)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Photo submissions must be sent as multipart/form-data.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var title = FirstNonEmpty(form["title"].ToString(), form["Title"].ToString());
        var description = FirstNonEmpty(form["description"].ToString(), form["Description"].ToString());
        var suggestedCategory = FirstNonEmpty(
            form["suggestedCategory"].ToString(),
            form["SuggestedCategory"].ToString());
        var approximateYear = TryParseInt(
            FirstNonEmpty(form["approximateYear"].ToString(), form["ApproximateYear"].ToString()));
        var approximateDate = TryParseDateOnly(
            FirstNonEmpty(form["approximateDate"].ToString(), form["ApproximateDate"].ToString()));
        var photo = FindPhotoFile(form.Files);
        if (photo is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "A photo file is required.");
        }

        await using var stream = photo.OpenReadStream();
        var result = await photoSubmissionService.SubmitAsync(
            memberId.Value,
            title ?? string.Empty,
            description,
            suggestedCategory,
            approximateYear,
            approximateDate,
            stream,
            photo.FileName,
            cancellationToken);

        if (!result.Succeeded || result.Submission is null)
        {
            return MapSubmitFailure(result.Error);
        }

        var submission = result.Submission;
        var dto = new PhotoSubmissionCreatedDto(
            submission.Id,
            submission.Status,
            submission.Title,
            submission.SubmittedAt);
        return Results.Created($"{PhotoSubmissionsPath}/{submission.Id:D}", dto);
    }

    internal static IResult MapSubmitFailure(string? error)
    {
        if (IsQuotaLimitError(error))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too Many Requests",
                detail: error);
        }

        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Bad Request",
            detail: error ?? "Could not submit photo.");
    }

    internal static bool IsQuotaLimitError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("Daily upload", StringComparison.Ordinal)
            || message.Contains("Uploads are temporarily disabled", StringComparison.Ordinal)
            || message.Contains("exceeds the daily size limit", StringComparison.Ordinal);
    }

    private static IFormFile? FindPhotoFile(IFormFileCollection files)
    {
        foreach (var name in new[] { "photo", "Photo", "photoFile", "PhotoFile" })
        {
            var file = files.GetFile(name);
            if (file is { Length: > 0 })
            {
                return file;
            }
        }

        return files.FirstOrDefault(file => file is { Length: > 0 });
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static int? TryParseInt(string? value) =>
        int.TryParse(value, out var parsed) ? parsed : null;

    private static DateOnly? TryParseDateOnly(string? value) =>
        DateOnly.TryParse(value, out var parsed) ? parsed : null;
}
