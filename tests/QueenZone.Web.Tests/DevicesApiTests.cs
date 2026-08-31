using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class DevicesApiTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private const string DevicesPath = "/api/v1/notifications/devices";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly QueenZoneWebApplicationFactory factory;

    public DevicesApiTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Register_RequiresMobileBearer()
    {
        using var client = factory.CreateAnonymousClient(allowAutoRedirect: false);

        using var response = await client.PostAsJsonAsync(
            DevicesPath,
            new { deviceId = "device-1", platform = "apns", token = "tok" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Register_NewDevice_StoresRow()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Device Fan", "device-new@example.com");
        using var client = CreateBearerClient(memberId, "Device Fan", "device-new@example.com");
        var deviceId = $"device-{Guid.NewGuid()}";

        using var response = await client.PostAsJsonAsync(
            DevicesPath,
            new { deviceId, platform = "apns", token = "token-a" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<DeviceRegisteredResponse>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(deviceId, dto!.DeviceId);
        Assert.Equal(PushDevicePlatform.Apns, dto.Platform);

        var stored = await FindDeviceAsync(deviceId);
        Assert.NotNull(stored);
        Assert.Equal(memberId, stored!.MemberAccountId);
        Assert.Equal("token-a", stored.Token);
    }

    [Fact]
    public async Task Register_SameDeviceId_UpdatesInPlace_NoDuplicate()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Rotate Fan", "device-rotate@example.com");
        using var client = CreateBearerClient(memberId, "Rotate Fan", "device-rotate@example.com");
        var deviceId = $"device-{Guid.NewGuid()}";

        using var first = await client.PostAsJsonAsync(
            DevicesPath,
            new { deviceId, platform = "fcm", token = "token-old" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var second = await client.PostAsJsonAsync(
            DevicesPath,
            new { deviceId, platform = "fcm", token = "token-new" });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var stored = await FindDeviceAsync(deviceId);
        Assert.NotNull(stored);
        Assert.Equal("token-new", stored!.Token);
        Assert.Equal(1, await CountDevicesAsync(deviceId));
    }

    [Fact]
    public async Task Register_SameDeviceId_ReassignsOwnerWhenDifferentMember()
    {
        var firstMemberId = Guid.NewGuid();
        var secondMemberId = Guid.NewGuid();
        await SeedMemberAsync(firstMemberId, "First Owner", "device-owner1@example.com");
        await SeedMemberAsync(secondMemberId, "Second Owner", "device-owner2@example.com");
        var deviceId = $"device-{Guid.NewGuid()}";

        using (var firstClient = CreateBearerClient(firstMemberId, "First Owner", "device-owner1@example.com"))
        {
            using var response = await firstClient.PostAsJsonAsync(
                DevicesPath,
                new { deviceId, platform = "apns", token = "token-1" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using (var secondClient = CreateBearerClient(secondMemberId, "Second Owner", "device-owner2@example.com"))
        {
            using var response = await secondClient.PostAsJsonAsync(
                DevicesPath,
                new { deviceId, platform = "apns", token = "token-2" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var stored = await FindDeviceAsync(deviceId);
        Assert.NotNull(stored);
        Assert.Equal(secondMemberId, stored!.MemberAccountId);
        Assert.Equal(1, await CountDevicesAsync(deviceId));
    }

    [Fact]
    public async Task Register_SameDeviceId_DifferentCasing_UpdatesInPlace_NoDuplicate()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Case Fan", "device-case@example.com");
        using var client = CreateBearerClient(memberId, "Case Fan", "device-case@example.com");
        const string storedDeviceId = "E3C869B0-F770-4EE4-BE4A-46C63CCBA90F";
        const string incomingDeviceId = "e3c869b0-f770-4ee4-be4a-46c63ccba90f";

        using var first = await client.PostAsJsonAsync(
            DevicesPath,
            new { deviceId = storedDeviceId, platform = "apns", token = "token-old" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var second = await client.PostAsJsonAsync(
            DevicesPath,
            new { deviceId = incomingDeviceId, platform = "fcm", token = "token-new" });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        Assert.Equal(1, await CountDevicesIgnoreCaseAsync(incomingDeviceId));
        var stored = await FindDeviceIgnoreCaseAsync(incomingDeviceId);
        Assert.NotNull(stored);
        Assert.Equal(memberId, stored!.MemberAccountId);
        Assert.Equal("token-new", stored.Token);
        Assert.Equal(QueenZone.Data.Entities.DevicePushPlatform.Fcm, stored.Platform);
    }

    [Theory]
    [InlineData(null, "apns", "tok")]
    [InlineData("", "apns", "tok")]
    [InlineData("device-1", null, "tok")]
    [InlineData("device-1", "apns", null)]
    [InlineData("device-1", "apns", "")]
    public async Task Register_MissingFields_ReturnsBadRequest(string? deviceId, string? platform, string? token)
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Bad Fan", $"bad-{Guid.NewGuid()}@example.com");
        using var client = CreateBearerClient(memberId, "Bad Fan", "bad-fan@example.com");

        using var response = await client.PostAsJsonAsync(
            DevicesPath,
            new { deviceId, platform, token });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Register_ResponseJson_KeepsDeviceRegistrationWireShape()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Contract Fan", "device-contract@example.com");
        using var client = CreateBearerClient(memberId, "Contract Fan", "device-contract@example.com");
        var deviceId = $"device-{Guid.NewGuid()}";

        using var response = await client.PostAsJsonAsync(
            DevicesPath,
            new { deviceId, platform = "apns", token = "token-contract" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal(
            ["deviceId", "platform", "updatedAt"],
            root.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(deviceId, root.GetProperty("deviceId").GetString());
        Assert.Equal("apns", root.GetProperty("platform").GetString());
        Assert.Equal(JsonValueKind.String, root.GetProperty("updatedAt").ValueKind);
        Assert.False(root.TryGetProperty("token", out _));
        Assert.False(root.TryGetProperty("memberAccountId", out _));
        Assert.False(root.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task Register_RequestJson_AcceptsExistingDeviceIdPlatformTokenShape()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Shape Fan", "device-shape@example.com");
        using var client = CreateBearerClient(memberId, "Shape Fan", "device-shape@example.com");
        using var content = new StringContent(
            """{"deviceId":"wire-device-1","platform":"fcm","token":"wire-token"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        using var response = await client.PostAsync(DevicesPath, content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("wire-device-1", doc.RootElement.GetProperty("deviceId").GetString());
        Assert.Equal("fcm", doc.RootElement.GetProperty("platform").GetString());
    }

    [Fact]
    public async Task Unregister_OwnDevice_RemovesRow()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Unregister Fan", "device-unreg@example.com");
        using var client = CreateBearerClient(memberId, "Unregister Fan", "device-unreg@example.com");
        var deviceId = $"device-{Guid.NewGuid()}";
        await client.PostAsJsonAsync(DevicesPath, new { deviceId, platform = "fcm", token = "tok" });

        using var response = await client.DeleteAsync($"{DevicesPath}/{deviceId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await FindDeviceAsync(deviceId));
    }

    [Fact]
    public async Task Unregister_UnknownDevice_ReturnsNotFound()
    {
        var memberId = Guid.NewGuid();
        await SeedMemberAsync(memberId, "Missing Fan", "device-missing@example.com");
        using var client = CreateBearerClient(memberId, "Missing Fan", "device-missing@example.com");

        using var response = await client.DeleteAsync($"{DevicesPath}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unregister_AnotherMembersDevice_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        await SeedMemberAsync(ownerId, "Owner Fan", "device-owner@example.com");
        await SeedMemberAsync(otherId, "Other Fan", "device-other@example.com");
        var deviceId = $"device-{Guid.NewGuid()}";

        using (var ownerClient = CreateBearerClient(ownerId, "Owner Fan", "device-owner@example.com"))
        {
            await ownerClient.PostAsJsonAsync(DevicesPath, new { deviceId, platform = "apns", token = "tok" });
        }

        using var otherClient = CreateBearerClient(otherId, "Other Fan", "device-other@example.com");
        using var response = await otherClient.DeleteAsync($"{DevicesPath}/{deviceId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(await FindDeviceAsync(deviceId));
    }

    private HttpClient CreateBearerClient(Guid memberId, string displayName, string email)
    {
        using var scope = factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<MobileAuthTokenIssuer>();
        var token = issuer.IssueAccessToken(memberId, email, displayName);
        var client = factory.CreateAnonymousClient(allowAutoRedirect: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task SeedMemberAsync(Guid memberId, string displayName, string email)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMemberAccountRepository>();
        await repository.CreateAsync(new MemberAccount
        {
            Id = memberId,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        });
    }

    private Task<DeviceTokenEntity?> FindDeviceAsync(string deviceId)
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<SharedDeviceTokenStore>();
        lock (store.Gate)
        {
            return Task.FromResult(store.Tokens.FirstOrDefault(token => token.DeviceId == deviceId));
        }
    }

    private Task<int> CountDevicesAsync(string deviceId)
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<SharedDeviceTokenStore>();
        lock (store.Gate)
        {
            return Task.FromResult(store.Tokens.Count(token => token.DeviceId == deviceId));
        }
    }

    private Task<DeviceTokenEntity?> FindDeviceIgnoreCaseAsync(string deviceId)
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<SharedDeviceTokenStore>();
        lock (store.Gate)
        {
            return Task.FromResult(store.Tokens.FirstOrDefault(
                token => string.Equals(token.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase)));
        }
    }

    private Task<int> CountDevicesIgnoreCaseAsync(string deviceId)
    {
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<SharedDeviceTokenStore>();
        lock (store.Gate)
        {
            return Task.FromResult(store.Tokens.Count(
                token => string.Equals(token.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
