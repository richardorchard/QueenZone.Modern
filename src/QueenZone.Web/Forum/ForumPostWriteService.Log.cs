using Microsoft.Extensions.Logging;

namespace QueenZone.Web;

public sealed partial class ForumPostWriteService
{
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1100,
            EventName = "PushDispatchFailedAfterForumReply",
            Level = LogLevel.Warning,
            Message = "Push dispatch failed after forum reply {TopicId}/{PostId} by member {MemberId}: {Error}")]
        public static partial void PushDispatchFailedAfterForumReply(
            ILogger logger,
            Exception exception,
            int topicId,
            int postId,
            Guid memberId,
            string error);

        [LoggerMessage(
            EventId = 1101,
            EventName = "AutoSuspendedMember",
            Level = LogLevel.Warning,
            Message = "Auto-suspended member {MemberId}: {Signature} {ElapsedSeconds:0}s after registration.")]
        public static partial void AutoSuspendedMember(
            ILogger logger,
            Guid memberId,
            string signature,
            double elapsedSeconds);
    }
}
