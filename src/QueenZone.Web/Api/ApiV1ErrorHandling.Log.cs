using Microsoft.Extensions.Logging;

namespace QueenZone.Web;

public static partial class ApiV1ErrorHandling
{
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1700,
            EventName = "UnhandledExceptionOnRequest",
            Level = LogLevel.Error,
            Message = "Unhandled exception on {Method} {Path}")]
        public static partial void UnhandledExceptionOnRequest(
            ILogger logger,
            Exception exception,
            string method,
            string path);
    }
}
