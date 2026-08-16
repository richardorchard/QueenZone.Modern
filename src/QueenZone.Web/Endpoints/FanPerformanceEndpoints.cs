using System.Net.Mime;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web;

public static class FanPerformanceEndpoints
{
    public static void MapFanPerformanceEndpoints(this WebApplication app)
    {
        app.MapGet("/fan-performances/{id:int}/audio/{filename?}", async (
            int id,
            IFanPerformanceRepository fanPerformanceRepository,
            IBlobUploadService blobUploadService,
            CancellationToken cancellationToken) =>
            await ServeAudioAsync(
                id,
                fanPerformanceRepository,
                blobUploadService,
                cancellationToken))
        .RequireAuthorization(MemberAuthenticationSchemes.MemberPolicy)
        .RequireRateLimiting(FanPerformanceRateLimitingOptions.AudioPolicy);
    }

    internal static async Task<IResult> ServeAudioAsync(
        int id,
        IFanPerformanceRepository fanPerformanceRepository,
        IBlobUploadService blobUploadService,
        CancellationToken cancellationToken)
    {
        var performance = await fanPerformanceRepository.GetByIdAsync(id, cancellationToken);
        if (performance is null)
        {
            return Results.NotFound();
        }

        if (!SongFileUrl.IsSafeBlobName(performance.AudioFileName))
        {
            return Results.NotFound();
        }

        var blobName = SongFileUrl.GetBlobName(performance.AudioFileName);
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return Results.NotFound();
        }

        try
        {
            var content = await blobUploadService.OpenReadAsync(
                SongFileUrl.ContainerName,
                blobName,
                cancellationToken);

            if (content is null)
            {
                return Results.NotFound();
            }

            var contentType = string.IsNullOrWhiteSpace(content.ContentType)
                ? MediaTypeNames.Application.Octet
                : content.ContentType;

            return Results.File(
                content.Stream,
                contentType,
                fileDownloadName: FanPerformanceRoutes.GetDownloadFileName(performance.Title),
                enableRangeProcessing: true);
        }
        catch (NotSupportedException)
        {
            return Results.NotFound();
        }
    }
}
