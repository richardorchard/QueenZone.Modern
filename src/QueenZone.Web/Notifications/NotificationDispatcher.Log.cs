using Microsoft.Extensions.Logging;

namespace QueenZone.Web;

public sealed partial class NotificationDispatcher
{
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1400,
            EventName = "PushDispatchFailedForCategory",
            Level = LogLevel.Warning,
            Message = "Push dispatch failed for category {Category}: {Error}")]
        public static partial void PushDispatchFailedForCategory(
            ILogger logger,
            Exception exception,
            string category,
            string error);
    }
}
