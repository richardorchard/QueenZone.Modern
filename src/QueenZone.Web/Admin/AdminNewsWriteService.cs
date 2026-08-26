using Microsoft.Extensions.Logging;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Shared admin news publish path. Dispatch fires only on unpublished → published.
/// </summary>
public sealed class AdminNewsWriteService(
    IAdminNewsRepository adminNewsRepository,
    INotificationDispatcher notificationDispatcher,
    ILogger<AdminNewsWriteService> logger)
{
    public async Task PublishAsync(
        AdminNewsArticle article,
        string editorEmail,
        CancellationToken cancellationToken = default)
    {
        var firstPublish = !article.IsPublished;
        await adminNewsRepository.PublishAsync(article.Id, editorEmail, cancellationToken);
        if (!firstPublish)
        {
            return;
        }

        try
        {
            await notificationDispatcher.NotifyNewsPublishedAsync(
                article.Id,
                article.Title,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Push dispatch failed after news publish {NewsId} for category {Category}: {Error}",
                article.Id,
                NotificationCategoryNames.News,
                ex.Message);
        }
    }
}
