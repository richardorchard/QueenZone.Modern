using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Opens one News-forum topic on first article publish and stores the link.
/// </summary>
public interface INewsForumTopicService
{
    Task EnsureTopicOnFirstPublishAsync(
        AdminNewsArticle article,
        CancellationToken cancellationToken = default);
}
