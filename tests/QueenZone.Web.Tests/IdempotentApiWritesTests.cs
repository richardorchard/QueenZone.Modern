using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class IdempotentApiWritesTests
{
    [Fact]
    public void TryReadKey_MissingOrBlank_IsOptional()
    {
        var missing = new DefaultHttpContext();
        Assert.True(IdempotentApiWrites.TryReadKey(missing.Request, out var none, out var missingError));
        Assert.Null(none);
        Assert.Null(missingError);

        var blank = new DefaultHttpContext();
        blank.Request.Headers[IdempotentApiWrites.HeaderName] = "  ";
        Assert.True(IdempotentApiWrites.TryReadKey(blank.Request, out var blankId, out var blankError));
        Assert.Null(blankId);
        Assert.Null(blankError);
    }

    [Fact]
    public void TryReadKey_ParsesUuid_AndRejectsInvalid()
    {
        var key = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var valid = new DefaultHttpContext();
        valid.Request.Headers[IdempotentApiWrites.HeaderName] = key.ToString("D");
        Assert.True(IdempotentApiWrites.TryReadKey(valid.Request, out var parsed, out var validError));
        Assert.Equal(key, parsed);
        Assert.Null(validError);

        var invalid = new DefaultHttpContext();
        invalid.Request.Headers[IdempotentApiWrites.HeaderName] = "nope";
        Assert.False(IdempotentApiWrites.TryReadKey(invalid.Request, out var bad, out var error));
        Assert.Null(bad);
        Assert.NotNull(error);
    }

    [Fact]
    public void Hash_AndPayloadHelpers_AreStable()
    {
        Assert.Equal(IdempotentApiWrites.Hash("a", "b"), IdempotentApiWrites.Hash("a", "b"));
        Assert.NotEqual(IdempotentApiWrites.Hash("a", "b"), IdempotentApiWrites.Hash("a", "c"));
        Assert.Equal(64, IdempotentApiWrites.ForumPayload(12, "Title", "Body", []).Length);
        var recipient = Guid.NewGuid();
        Assert.Equal(
            IdempotentApiWrites.MessageComposePayload(recipient, "Hi"),
            IdempotentApiWrites.MessageComposePayload(recipient, "Hi"));
        var conversation = Guid.NewGuid();
        Assert.NotEqual(
            IdempotentApiWrites.MessageReplyPayload(conversation, "A"),
            IdempotentApiWrites.MessageReplyPayload(conversation, "B"));
    }

    [Fact]
    public async Task Created_SerializesCamelCaseBody_AndReplayWritesIt()
    {
        var services = new ServiceCollection();
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        await using var provider = services.BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = provider };
        var dto = new ForumPostCreatedDto(9, 1002, "/forum/topic/1002#post-9");
        var write = IdempotentApiWrites.Created(http.Request, dto.DetailPath, dto, "hash");

        Assert.Equal(201, write.Receipt!.StatusCode);
        Assert.Equal(dto.DetailPath, write.Receipt.Location);
        Assert.Contains("\"id\":9", write.Receipt.ResponseBodyJson, StringComparison.Ordinal);
        Assert.Contains("\"topicId\":1002", write.Receipt.ResponseBodyJson, StringComparison.Ordinal);

        var replay = new DefaultHttpContext();
        replay.Response.Body = new MemoryStream();
        await new IdempotentReplayResult(write.Receipt).ExecuteAsync(replay);
        replay.Response.Body.Position = 0;
        using var reader = new StreamReader(replay.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Equal(201, replay.Response.StatusCode);
        Assert.Equal(dto.DetailPath, replay.Response.Headers.Location);
        Assert.Equal(write.Receipt.ResponseBodyJson, body);
        Assert.Equal("no-store", replay.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task Execute_InvalidKey_DoesNotCallWrite()
    {
        var store = new InMemoryIdempotencyStore();
        var http = new DefaultHttpContext();
        http.Request.Headers[IdempotentApiWrites.HeaderName] = "bad";
        var calls = 0;
        var result = await IdempotentApiWrites.ExecuteAsync(
            http.Request,
            Guid.NewGuid(),
            IdempotencyOperationKinds.ForumCreateReply,
            "hash",
            store,
            _ =>
            {
                calls++;
                return Task.FromResult(new IdempotentWrite(Results.Ok(), null));
            },
            CancellationToken.None);

        Assert.Equal(0, calls);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Execute_WithoutKey_RunsWriteOnce()
    {
        var store = new InMemoryIdempotencyStore();
        var http = new DefaultHttpContext();
        var calls = 0;
        var result = await IdempotentApiWrites.ExecuteAsync(
            http.Request,
            Guid.NewGuid(),
            IdempotencyOperationKinds.ForumCreateReply,
            "hash",
            store,
            _ =>
            {
                calls++;
                return Task.FromResult(new IdempotentWrite(Results.Ok("ran"), null));
            },
            CancellationToken.None);

        Assert.Equal(1, calls);
        Assert.IsAssignableFrom<IResult>(result);
    }

    [Fact]
    public void Conflict_UsesProblemDetails()
    {
        var conflict = IdempotentApiWrites.Conflict();
        var status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(conflict);
        Assert.Equal(StatusCodes.Status409Conflict, status.StatusCode);
    }
}
