using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web;

public static class MemberAvatarEndpoints
{
    public const string Route = "/account/avatar/{memberId:guid}";

    public static void MapMemberAvatarEndpoints(this WebApplication app)
    {
        app.MapGet(Route, async (
                Guid memberId,
                [FromQuery] string? size,
                [FromServices] IMemberAccountRepository memberAccounts,
                [FromServices] IBlobUploadService blobUploadService,
                CancellationToken cancellationToken) =>
            await ServeAvatarAsync(memberId, size, memberAccounts, blobUploadService, cancellationToken))
            .WithName("MemberAvatar")
            .AllowAnonymous();
    }

    internal static async Task<IResult> ServeAvatarAsync(
        Guid memberId,
        string? size,
        IMemberAccountRepository memberAccounts,
        IBlobUploadService blobUploadService,
        CancellationToken cancellationToken)
    {
        var account = await memberAccounts.FindByIdAsync(memberId, cancellationToken);
        if (account is null || string.IsNullOrWhiteSpace(account.AvatarUrl))
        {
            return Results.NotFound();
        }

        var useThumb = string.Equals(size, "thumb", StringComparison.OrdinalIgnoreCase);
        var blobName = useThumb
            ? MemberAvatarPaths.ToThumbBlobName(account.AvatarUrl)
            : account.AvatarUrl;

        try
        {
            var content = await blobUploadService.OpenReadAsync(
                MemberAvatarPaths.Container,
                blobName,
                cancellationToken);

            // Fall back to full avatar when thumb is missing.
            if (content is null && useThumb)
            {
                content = await blobUploadService.OpenReadAsync(
                    MemberAvatarPaths.Container,
                    account.AvatarUrl,
                    cancellationToken);
            }

            if (content is null)
            {
                return Results.NotFound();
            }

            return Results.Stream(
                content.Stream,
                content.ContentType,
                enableRangeProcessing: false);
        }
        catch (NotSupportedException)
        {
            return Results.NotFound();
        }
    }
}
