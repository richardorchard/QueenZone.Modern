using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Limits private message sends per member: overall volume, identical-content repetition, and
/// distinct-recipient fan-out for new conversations. Newer accounts use stricter thresholds.
/// When a DB probe fails, sends are <b>denied</b> (fail-closed) so a database outage cannot
/// become an open spam window, mirroring <see cref="ForumPostRateLimiter"/>.
/// </summary>
public sealed class PrivateMessageRateLimiter(
    IPrivateMessageRepository privateMessageRepository,
    TimeProvider timeProvider,
    IOptions<PrivateMessageRateLimitOptions> options,
    ILogger<PrivateMessageRateLimiter> logger)
{
    public async Task<bool> IsSendAllowedAsync(
        Guid senderMemberId,
        DateTime senderCreatedAtUtc,
        string body,
        bool isNewConversation,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var now = timeProvider.GetUtcNow();
        var window = TimeSpan.FromMinutes(Math.Max(1, opts.WindowMinutes));
        var since = now - window;

        var accountAge = now - new DateTimeOffset(DateTime.SpecifyKind(senderCreatedAtUtc, DateTimeKind.Utc));
        var isNewAccount = accountAge <= TimeSpan.FromDays(Math.Max(0, opts.NewAccountAgeDays));

        var maxMessages = isNewAccount ? opts.NewAccountMaxMessagesPerWindow : opts.MaxMessagesPerWindow;
        var maxNewRecipients = isNewAccount
            ? opts.NewAccountMaxNewRecipientsPerWindow
            : opts.MaxNewRecipientsPerWindow;

        try
        {
            var messageCount = await privateMessageRepository.CountMessagesBySenderSinceAsync(
                senderMemberId,
                since,
                cancellationToken);
            if (messageCount >= maxMessages)
            {
                return false;
            }

            var duplicateCount = await privateMessageRepository.CountIdenticalMessagesBySenderSinceAsync(
                senderMemberId,
                body,
                since,
                cancellationToken);
            if (duplicateCount >= opts.MaxDuplicateMessagesPerWindow)
            {
                return false;
            }

            if (isNewConversation)
            {
                var recipientCount = await privateMessageRepository.CountDistinctNewRecipientsSinceAsync(
                    senderMemberId,
                    since,
                    cancellationToken);
                if (recipientCount >= maxNewRecipients)
                {
                    return false;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            // Fail-closed: do not allow sends when we cannot verify the rate limit.
            logger.LogWarning(
                ex,
                "Private message rate-limit probe failed for member {MemberId}; denying this send.",
                senderMemberId);
            return false;
        }

        return true;
    }
}
