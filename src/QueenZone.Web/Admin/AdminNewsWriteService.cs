using Microsoft.Extensions.Logging;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Shared admin news publish path. Dispatch and News-forum topic create fire only on
/// unpublished → published. Topic create is fail-open, like push.
/// </summary>
public sealed class AdminNewsWriteService(
    IAdminNewsRepository adminNewsRepository,
    INotificationDispatcher notificationDispatcher,
    INewsForumTopicService newsForumTopicService,
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
            await newsForumTopicService.EnsureTopicOnFirstPublishAsync(article, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "News forum topic create failed after news publish {NewsId}: {Error}",
                article.Id,
                ex.Message);
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
