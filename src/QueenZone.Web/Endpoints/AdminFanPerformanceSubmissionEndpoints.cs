using System.Net.Mime;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web;

public static class AdminFanPerformanceSubmissionEndpoints
{
    public static void MapAdminFanPerformanceSubmissionEndpoints(this WebApplication app)
    {
        app.MapGet("/admin/fan-performance-submissions/{id:guid}/audio", ServePendingAudioAsync)
            .RequireAuthorization(AdminAuthenticationSchemes.Policy);
    }

    internal static async Task<IResult> ServePendingAudioAsync(
        Guid id,
        IFanPerformanceSubmissionRepository fanPerformanceSubmissionRepository,
        IBlobUploadService blobUploadService,
        CancellationToken cancellationToken)
    {
        var submission = await fanPerformanceSubmissionRepository.GetByIdAsync(id, cancellationToken);
        if (submission is null || string.IsNullOrWhiteSpace(submission.BlobPath))
        {
            return Results.NotFound();
        }

        try
        {
            var content = await blobUploadService.OpenReadAsync(
                BlobUploadContainers.FanPerformances,
                submission.BlobPath,
                cancellationToken);
            if (content is null)
            {
                return Results.NotFound();
            }

            var contentType = string.IsNullOrWhiteSpace(content.ContentType)
                ? MediaTypeNames.Application.Octet
                : content.ContentType;

            return Results.File(content.Stream, contentType, enableRangeProcessing: true);
        }
        catch (NotSupportedException)
        {
            return Results.NotFound();
        }
    }
}
