using Microsoft.Extensions.Logging;

namespace QueenZone.Web;

internal sealed partial class DirectPushTransport
{
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1500,
            EventName = "ApnsCredentialsNotConfigured",
            Level = LogLevel.Warning,
            Message = "PushNotifications APNs credentials are not configured; skipping APNs sends for category {Category}.")]
        public static partial void ApnsCredentialsNotConfigured(ILogger logger, string category);

        [LoggerMessage(
            EventId = 1501,
            EventName = "ApnsSendFailed",
            Level = LogLevel.Warning,
            Message = "APNs send failed for member {MemberId} category {Category}: {Error}")]
        public static partial void ApnsSendFailed(
            ILogger logger,
            Guid memberId,
            string category,
            string error);

        [LoggerMessage(
            EventId = 1502,
            EventName = "ApnsSendFailedWithException",
            Level = LogLevel.Warning,
            Message = "APNs send failed for member {MemberId} category {Category}: {Error}")]
        public static partial void ApnsSendFailedWithException(
            ILogger logger,
            Exception exception,
            Guid memberId,
            string category,
            string error);

        [LoggerMessage(
            EventId = 1503,
            EventName = "FcmCredentialsNotConfigured",
            Level = LogLevel.Warning,
            Message = "PushNotifications FCM credentials are not configured; skipping FCM sends for category {Category}.")]
        public static partial void FcmCredentialsNotConfigured(ILogger logger, string category);

        [LoggerMessage(
            EventId = 1504,
            EventName = "FcmAccessTokenNotConfigured",
            Level = LogLevel.Warning,
            Message = "PushNotifications FCM credentials are not configured; skipping FCM sends for category {Category}.")]
        public static partial void FcmAccessTokenNotConfigured(ILogger logger, string category);

        [LoggerMessage(
            EventId = 1505,
            EventName = "FcmSendFailed",
            Level = LogLevel.Warning,
            Message = "FCM send failed for member {MemberId} category {Category}: {Error}")]
        public static partial void FcmSendFailed(
            ILogger logger,
            Guid memberId,
            string category,
            string error);

        [LoggerMessage(
            EventId = 1506,
            EventName = "FcmSendFailedWithException",
            Level = LogLevel.Warning,
            Message = "FCM send failed for member {MemberId} category {Category}: {Error}")]
        public static partial void FcmSendFailedWithException(
            ILogger logger,
            Exception exception,
            Guid memberId,
            string category,
            string error);
    }
}
