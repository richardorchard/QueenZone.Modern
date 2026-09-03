using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QueenZone.Web;

internal sealed class GoogleFcmAccessTokenProvider(
    IOptions<PushNotificationOptions> options,
    ILogger<GoogleFcmAccessTokenProvider> logger) : IFcmAccessTokenProvider
{
    private const string MessagingScope = "https://www.googleapis.com/auth/firebase.messaging";

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var fcm = options.Value.Fcm;
        if (!OptionsValidation.LooksConfigured(fcm.ProjectId)
            || !OptionsValidation.LooksConfigured(fcm.ServiceAccountJson))
        {
            return null;
        }

        try
        {
            var credential = CredentialFactory
                .FromJson<ServiceAccountCredential>(fcm.ServiceAccountJson)
                .ToGoogleCredential()
                .CreateScoped(MessagingScope);
            return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "FCM access token could not be minted; skipping FCM sends.");
            return null;
        }
    }
}
