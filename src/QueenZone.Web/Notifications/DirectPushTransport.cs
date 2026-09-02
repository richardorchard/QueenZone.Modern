using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QueenZone.Web;

internal sealed class DirectPushTransport(
    IHttpClientFactory httpClientFactory,
    IOptions<PushNotificationOptions> options,
    IFcmAccessTokenProvider fcmAccessTokenProvider,
    ILogger<DirectPushTransport> logger) : IPushTransport
{
    public const string ApnsClientName = "ApnsPush";

    public const string FcmClientName = "FcmPush";

    internal const string ProductionApnsHost = "https://api.push.apple.com";

    internal const string SandboxApnsHost = "https://api.sandbox.push.apple.com";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ApnsJwtFactory apnsJwtFactory = new();

    public async Task SendAsync(
        IReadOnlyList<PushDeviceToken> tokens,
        PushNotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (tokens.Count == 0)
        {
            return;
        }

        var apns = tokens.Where(token => token.Platform == PushDevicePlatform.Apns).ToList();
        var fcm = tokens.Where(token => token.Platform == PushDevicePlatform.Fcm).ToList();

        if (apns.Count > 0)
        {
            await SendApnsAsync(apns, payload, cancellationToken);
        }

        if (fcm.Count > 0)
        {
            await SendFcmAsync(fcm, payload, cancellationToken);
        }
    }

    private async Task SendApnsAsync(
        IReadOnlyList<PushDeviceToken> tokens,
        PushNotificationPayload payload,
        CancellationToken cancellationToken)
    {
        var apns = options.Value.Apns;
        var jwt = apnsJwtFactory.TryCreateToken(apns);
        if (jwt is null)
        {
            logger.LogWarning(
                "PushNotifications APNs credentials are not configured; skipping APNs sends for category {Category}.",
                payload.Category);
            return;
        }

        var host = IsSandbox(apns.Environment) ? SandboxApnsHost : ProductionApnsHost;
        var topic = string.IsNullOrWhiteSpace(apns.Topic)
            ? PushNotificationOptions.DefaultApnsTopic
            : apns.Topic.Trim();
        var client = httpClientFactory.CreateClient(ApnsClientName);
        var body = BuildApnsBody(payload);
        using var gate = new SemaphoreSlim(PushNotificationOptions.DefaultApnsMaxConcurrency);

        var sends = tokens.Select(async token =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                await SendOneApnsAsync(client, host, topic, jwt, token, body, payload.Category, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(sends);
    }

    private async Task SendOneApnsAsync(
        HttpClient client,
        string host,
        string topic,
        string jwt,
        PushDeviceToken device,
        string body,
        string category,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{host}/3/device/{device.Token}");
        request.Version = HttpVersion.Version20;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", jwt);
        request.Headers.TryAddWithoutValidation("apns-topic", topic);
        request.Headers.TryAddWithoutValidation("apns-push-type", "alert");
        request.Headers.TryAddWithoutValidation("apns-priority", "10");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var error = await ReadProviderErrorAsync(response, device.Token, cancellationToken);
            logger.LogWarning(
                "APNs send failed for member {MemberId} category {Category}: {Error}",
                device.MemberAccountId,
                category,
                error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "APNs send failed for member {MemberId} category {Category}: {Error}",
                device.MemberAccountId,
                category,
                ex.Message);
        }
    }

    private async Task SendFcmAsync(
        IReadOnlyList<PushDeviceToken> tokens,
        PushNotificationPayload payload,
        CancellationToken cancellationToken)
    {
        var projectId = options.Value.Fcm.ProjectId?.Trim();
        if (!OptionsValidation.LooksConfigured(projectId))
        {
            logger.LogWarning(
                "PushNotifications FCM credentials are not configured; skipping FCM sends for category {Category}.",
                payload.Category);
            return;
        }

        var accessToken = await fcmAccessTokenProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning(
                "PushNotifications FCM credentials are not configured; skipping FCM sends for category {Category}.",
                payload.Category);
            return;
        }

        var client = httpClientFactory.CreateClient(FcmClientName);
        var url = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";

        foreach (var batch in tokens.Chunk(PushNotificationOptions.DefaultFcmBatchSize))
        {
            var sends = batch.Select(token =>
                SendOneFcmAsync(client, url, accessToken, token, payload, cancellationToken));
            await Task.WhenAll(sends);
        }
    }

    private async Task SendOneFcmAsync(
        HttpClient client,
        string url,
        string accessToken,
        PushDeviceToken device,
        PushNotificationPayload payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(BuildFcmBody(device.Token, payload), Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var error = await ReadProviderErrorAsync(response, device.Token, cancellationToken);
            logger.LogWarning(
                "FCM send failed for member {MemberId} category {Category}: {Error}",
                device.MemberAccountId,
                payload.Category,
                error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "FCM send failed for member {MemberId} category {Category}: {Error}",
                device.MemberAccountId,
                payload.Category,
                ex.Message);
        }
    }

    internal static string BuildApnsBody(PushNotificationPayload payload)
    {
        var document = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["aps"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["alert"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["title"] = payload.Title,
                    ["body"] = payload.Body,
                },
                ["sound"] = "default",
            },
        };

        foreach (var pair in payload.Data)
        {
            document[pair.Key] = pair.Value;
        }

        return JsonSerializer.Serialize(document, JsonOptions);
    }

    internal static string BuildFcmBody(string deviceToken, PushNotificationPayload payload)
    {
        var document = new
        {
            message = new
            {
                token = deviceToken,
                notification = new { title = payload.Title, body = payload.Body },
                data = payload.Data,
            },
        };

        return JsonSerializer.Serialize(document, JsonOptions);
    }

    internal static bool IsSandbox(string? environment) =>
        string.Equals(environment?.Trim(), "sandbox", StringComparison.OrdinalIgnoreCase);

    private static async Task<string> ReadProviderErrorAsync(
        HttpResponseMessage response,
        string deviceToken,
        CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var sanitized = RedactToken(raw, deviceToken);
        return $"{(int)response.StatusCode} {sanitized}";
    }

    internal static string RedactToken(string? text, string token)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token))
        {
            return string.IsNullOrEmpty(text) ? "unknown" : text;
        }

        return text.Replace(token, "[redacted]", StringComparison.Ordinal);
    }
}
