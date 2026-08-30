using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web;

internal readonly record struct IdempotentWrite(IResult Result, IdempotencyReceipt? Receipt);

internal static class IdempotentApiWrites
{
    public const string HeaderName = "Idempotency-Key";

    public const string InvalidKeyDetail = "Idempotency-Key must be a UUID.";

    public const string ConflictDetail = "Idempotency-Key was reused with a different payload.";

    public static async Task<IResult> ExecuteAsync(
        HttpRequest request,
        Guid memberId,
        string operationKind,
        string payloadHash,
        IIdempotencyStore store,
        Func<CancellationToken, Task<IdempotentWrite>> write,
        CancellationToken cancellationToken)
    {
        if (!TryReadKey(request, out var operationId, out var error))
        {
            return error!;
        }

        if (operationId is null)
        {
            var ran = await write(cancellationToken);
            return ran.Result;
        }

        var outcome = await store.ExecuteAsync(
            memberId,
            operationKind,
            operationId.Value,
            payloadHash,
            async ct =>
            {
                var ran = await write(ct);
                return (ran.Result, ran.Receipt);
            },
            cancellationToken);

        return outcome.Kind switch
        {
            IdempotencyExecuteKind.Replay => new IdempotentReplayResult(outcome.Receipt!),
            IdempotencyExecuteKind.Conflict => Conflict(),
            IdempotencyExecuteKind.Ran => outcome.Result!,
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Server Error",
                detail: "Unable to complete this request."),
        };
    }

    public static bool TryReadKey(HttpRequest request, out Guid? operationId, out IResult? error)
    {
        operationId = null;
        error = null;
        if (!request.Headers.TryGetValue(HeaderName, out var values))
        {
            return true;
        }

        var raw = values.ToString().Trim();
        if (raw.Length == 0)
        {
            return true;
        }

        if (!Guid.TryParse(raw, out var parsed) || parsed == Guid.Empty)
        {
            error = Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: InvalidKeyDetail);
            return false;
        }

        operationId = parsed;
        return true;
    }

    public static IdempotentWrite Created<T>(HttpRequest request, string location, T body, string payloadHash)
    {
        var json = Serialize(request, body);
        return new IdempotentWrite(
            Results.Created(location, body),
            new IdempotencyReceipt(StatusCodes.Status201Created, location, json, payloadHash));
    }

    public static string Hash(params string?[] parts)
    {
        var canonical = string.Join('\u001f', parts.Select(static part => part ?? string.Empty));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static string ForumPayload(int targetId, string? title, string? body, IReadOnlyList<IFormFile> files)
    {
        var filePart = string.Join(
            '|',
            files.Select(file => $"{file.FileName}:{file.Length}:{file.ContentType}"));
        return Hash(targetId.ToString(System.Globalization.CultureInfo.InvariantCulture), title, body, filePart);
    }

    public static string MessageComposePayload(Guid recipientMemberId, string? body) =>
        Hash(recipientMemberId.ToString("D"), body);

    public static string MessageReplyPayload(Guid conversationId, string? body) =>
        Hash(conversationId.ToString("D"), body);

    public static IResult Conflict() =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflict",
            detail: ConflictDetail);

    private static string Serialize<T>(HttpRequest request, T body)
    {
        var options = request.HttpContext.RequestServices
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value.SerializerOptions;
        return JsonSerializer.Serialize(body, options);
    }
}

internal sealed class IdempotentReplayResult(IdempotencyReceipt receipt) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = receipt.StatusCode;
        if (!string.IsNullOrEmpty(receipt.Location))
        {
            httpContext.Response.Headers.Location = receipt.Location;
        }

        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        return httpContext.Response.WriteAsync(receipt.ResponseBodyJson, httpContext.RequestAborted);
    }
}
