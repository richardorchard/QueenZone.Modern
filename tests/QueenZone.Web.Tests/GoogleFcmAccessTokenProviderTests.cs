using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class GoogleFcmAccessTokenProviderTests
{
    [Fact]
    public async Task MissingCredentials_ReturnsNull()
    {
        var provider = new GoogleFcmAccessTokenProvider(
            Options.Create(new PushNotificationOptions()),
            NullLogger<GoogleFcmAccessTokenProvider>.Instance);

        Assert.Null(await provider.GetAccessTokenAsync());
    }

    [Fact]
    public async Task InvalidServiceAccountJson_ReturnsNull()
    {
        var provider = new GoogleFcmAccessTokenProvider(
            Options.Create(new PushNotificationOptions
            {
                Fcm = new FcmPushOptions
                {
                    ProjectId = "queenzone-mobile",
                    ServiceAccountJson = "{ not-json",
                },
            }),
            NullLogger<GoogleFcmAccessTokenProvider>.Instance);

        Assert.Null(await provider.GetAccessTokenAsync());
    }
}
