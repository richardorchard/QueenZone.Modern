using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Storage;
using QueenZone.Web;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace QueenZone.Web.Tests;

public sealed partial class AccountSettingsPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly InMemoryBlobStorageBackend blobBackend = new();

    public AccountSettingsPageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services
                    .AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, ExternalCookieTestHandler>(
                        MemberAuthenticationSchemes.ExternalCookie, _ => { });

                // Swap in a backend this test can inspect directly (default Testing composition
                // also uses an in-memory-backed AzureBlobUploadService, but not this instance).
                services.RemoveAll<IBlobUploadService>();
                services.AddSingleton<IBlobUploadService>(_ =>
                    new AzureBlobUploadService(blobBackend, Options.Create(new BlobUploadOptions())));
            });
        });
    }

    [Fact]
    public async Task Get_RedirectsUnauthenticatedUsersToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/account/settings");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Get_ShowsPrefilledForm_ForAuthenticatedMember()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "settings-get@example.com",
            displayName: "Settings Fan",
            subject: "google-settings-get");

        var body = await client.GetStringAsync("/account/settings");

        Assert.Contains("Account settings", body);
        Assert.Contains("settings-get@example.com", body);
        Assert.Contains("value=\"Settings Fan\"", body);
        Assert.Contains("Google", body);
        Assert.Contains("name=\"DisplayName\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Save display name", body);
        Assert.Contains("Legacy forum account", body);
        Assert.Contains("No legacy forum account is linked to this email.", body);
    }

    [Fact]
    public async Task Get_ShowsLegacyClaimOffer_WhenEmailMatchesLegacyAccount()
    {
        var client = await CreateSignedInMemberClientWithLegacyMatchAsync(
            email: "claim-offer@example.com",
            displayName: "Modern Fan",
            subject: "google-legacy-claim-offer",
            legacyUserId: 9001,
            legacyUsername: "ArchiveOffer");

        var body = await client.GetStringAsync("/account/settings");

        Assert.Contains("ArchiveOffer", body);
        Assert.Contains("Claim legacy account", body);
        Assert.Contains("Use legacy username", body);
        Assert.DoesNotContain("Linked to legacy forum account", body);
    }

    [Fact]
    public async Task PostClaimLegacy_LinksAccount_AndShowsLinkedStatus()
    {
        var client = await CreateSignedInMemberClientWithLegacyMatchAsync(
            email: "claim-post@example.com",
            displayName: "Modern Fan",
            subject: "google-legacy-claim-post",
            legacyUserId: 9002,
            legacyUsername: "ArchiveClaimed",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/account/settings");
        Assert.Contains("Claim legacy account", formPage);

        var response = await client.PostAsync(
            "/account/settings?handler=ClaimLegacy",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
                ["SelectedLegacyUserId"] = "9002",
                ["AdoptLegacyDisplayName"] = "true",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/settings", response.Headers.Location!.OriginalString);

        var updated = await client.GetStringAsync("/account/settings");
        Assert.Contains("Legacy account claimed", updated);
        Assert.Contains("Linked to legacy forum account", updated);
        Assert.Contains("ArchiveClaimed", updated);
        Assert.Contains("Unlink legacy account", updated);
        Assert.DoesNotContain("Claim legacy account", updated);
        Assert.Contains("value=\"ArchiveClaimed\"", updated);
    }

    [Fact]
    public async Task PostUnlinkLegacy_ClearsLink_AndShowsClaimOfferAgain()
    {
        var client = await CreateSignedInMemberClientWithLegacyMatchAsync(
            email: "unlink-post@example.com",
            displayName: "Modern Fan",
            subject: "google-legacy-unlink-post",
            legacyUserId: 9003,
            legacyUsername: "ArchiveUnlink",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var claimPage = await client.GetStringAsync("/account/settings");
        var claimResponse = await client.PostAsync(
            "/account/settings?handler=ClaimLegacy",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(claimPage),
                ["SelectedLegacyUserId"] = "9003",
                ["AdoptLegacyDisplayName"] = "false",
            }));
        Assert.Equal(HttpStatusCode.Redirect, claimResponse.StatusCode);

        var linkedPage = await client.GetStringAsync("/account/settings");
        Assert.Contains("Unlink legacy account", linkedPage);
        Assert.Contains("ArchiveUnlink", linkedPage);

        var unlinkResponse = await client.PostAsync(
            "/account/settings?handler=UnlinkLegacy",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(linkedPage),
            }));

        Assert.Equal(HttpStatusCode.Redirect, unlinkResponse.StatusCode);
        Assert.Equal("/account/settings", unlinkResponse.Headers.Location!.OriginalString);

        var unlinked = await client.GetStringAsync("/account/settings");
        Assert.Contains("Legacy account unlinked", unlinked);
        Assert.Contains("Claim legacy account", unlinked);
        Assert.Contains("ArchiveUnlink", unlinked);
        Assert.DoesNotContain("Linked to legacy forum account", unlinked);
        Assert.DoesNotContain("Unlink legacy account", unlinked);
    }

    [Fact]
    public async Task Get_ShowsLegacyChoiceList_WhenEmailMatchesMultipleAccounts()
    {
        var client = await CreateSignedInMemberClientWithLegacyMatchesAsync(
            email: "multi-claim@example.com",
            displayName: "Modern Fan",
            subject: "google-legacy-multi-claim-get",
            matches:
            [
                new LegacyMemberMatch(9101, "FirstArchive"),
                new LegacyMemberMatch(9102, "SecondArchive"),
            ]);

        var body = await client.GetStringAsync("/account/settings");

        Assert.Contains("multiple classic QueenZone forum accounts", body);
        Assert.Contains("FirstArchive", body);
        Assert.Contains("SecondArchive", body);
        Assert.Contains("name=\"SelectedLegacyUserId\"", body);
        Assert.Contains("value=\"9101\"", body);
        Assert.Contains("value=\"9102\"", body);
        Assert.Contains("Claim legacy account", body);
    }

    [Fact]
    public async Task PostClaimLegacy_ClaimsSelectedAccount_WhenMultipleMatches()
    {
        var client = await CreateSignedInMemberClientWithLegacyMatchesAsync(
            email: "multi-claim-post@example.com",
            displayName: "Modern Fan",
            subject: "google-legacy-multi-claim-post",
            matches:
            [
                new LegacyMemberMatch(9201, "KeepMe"),
                new LegacyMemberMatch(9202, "PickMe"),
            ],
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/account/settings");
        Assert.Contains("PickMe", formPage);

        var response = await client.PostAsync(
            "/account/settings?handler=ClaimLegacy",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
                ["SelectedLegacyUserId"] = "9202",
                ["AdoptLegacyDisplayName"] = "true",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var updated = await client.GetStringAsync("/account/settings");
        Assert.Contains("Legacy account claimed", updated);
        Assert.Contains("Linked to legacy forum account", updated);
        Assert.Contains("PickMe", updated);
        Assert.Contains("(id 9202)", updated);
        Assert.DoesNotContain("Claim legacy account", updated);
        Assert.Contains("value=\"PickMe\"", updated);
    }

    [Fact]
    public async Task Post_ValidName_SucceedsAndReissuesCookieWithUpdatedClaim()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "settings-post@example.com",
            displayName: "Before Name",
            subject: "google-settings-post",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/account/settings");
        Assert.Contains("value=\"Before Name\"", formPage);

        var response = await client.PostAsync(
            "/account/settings",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
                ["DisplayName"] = "  After Name  ",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/settings", response.Headers.Location!.OriginalString);

        // Follow redirect; header should show the new display name from the re-issued cookie.
        var updatedPage = await client.GetStringAsync("/account/settings");
        Assert.Contains("Display name updated.", updatedPage);
        Assert.Contains("After Name", updatedPage, StringComparison.Ordinal);
        Assert.DoesNotContain("Before Name", updatedPage, StringComparison.Ordinal);
        Assert.Contains("name=\"DisplayName\"", updatedPage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_InvalidName_ReturnsValidationErrors()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "settings-invalid@example.com",
            displayName: "Valid Name",
            subject: "google-settings-invalid",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/account/settings");
        var response = await client.PostAsync(
            "/account/settings",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
                ["DisplayName"] = " ",
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("required", body, StringComparison.OrdinalIgnoreCase);
        // Form value retained (empty after trim may show empty input; original name should not be silently reapplied as success).
        Assert.DoesNotContain("Display name updated.", body);
    }

    [Fact]
    public async Task Post_TooShortName_ReturnsValidationErrors()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "settings-short@example.com",
            displayName: "Valid Name",
            subject: "google-settings-short",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/account/settings");
        var response = await client.PostAsync(
            "/account/settings",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
                ["DisplayName"] = "A",
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("between", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Display name updated.", body);
        Assert.Contains("value=\"A\"", body);
    }

    [Fact]
    public async Task SignedInHeader_IncludesSettingsLink()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "settings-nav@example.com",
            displayName: "Nav Fan",
            subject: "google-settings-nav");

        var body = await client.GetStringAsync("/fan-performances");

        Assert.Contains("href=\"/account/settings\"", body);
        Assert.Contains(">Settings</a>", body);
        Assert.Contains("qz-avatar", body);
    }

    [Fact]
    public async Task PostUploadAvatar_StoresAvatar_AndShowsImageOnSettings()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "settings-avatar@example.com",
            displayName: "Avatar Uploader",
            subject: "google-settings-avatar",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/account/settings");
        var token = ExtractAntiforgeryToken(formPage);
        await using var png = await CreatePngAsync();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(token), "__RequestVerificationToken");
        var fileContent = new StreamContent(png);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "AvatarFile", "face.png");

        var response = await client.PostAsync("/account/settings?handler=UploadAvatar", content);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var updated = await client.GetStringAsync("/account/settings");
        Assert.Contains("Avatar updated.", updated);
        Assert.Contains("/account/avatar/", updated);
        Assert.Contains("Remove avatar", updated);
    }

    [Fact]
    public async Task PostRemoveAvatar_ClearsAvatar()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "settings-remove-avatar@example.com",
            displayName: "Remove Avatar Fan",
            subject: "google-settings-remove-avatar",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        // Upload first.
        var formPage = await client.GetStringAsync("/account/settings");
        await using var png = await CreatePngAsync();
        using (var content = new MultipartFormDataContent())
        {
            content.Add(new StringContent(ExtractAntiforgeryToken(formPage)), "__RequestVerificationToken");
            var fileContent = new StreamContent(png);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "AvatarFile", "face.png");
            var upload = await client.PostAsync("/account/settings?handler=UploadAvatar", content);
            Assert.Equal(HttpStatusCode.Redirect, upload.StatusCode);
        }

        var afterUpload = await client.GetStringAsync("/account/settings");
        Assert.Contains("Remove avatar", afterUpload);
        using var remove = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(afterUpload),
        });
        var removeResponse = await client.PostAsync("/account/settings?handler=RemoveAvatar", remove);
        Assert.Equal(HttpStatusCode.Redirect, removeResponse.StatusCode);

        var afterRemove = await client.GetStringAsync("/account/settings");
        Assert.Contains("Avatar removed.", afterRemove);
        Assert.DoesNotContain("Remove avatar", afterRemove);
    }

    private static async Task<MemoryStream> CreatePngAsync()
    {
        using var image = new Image<Rgba32>(40, 40, new Rgba32(10, 180, 90));
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }

    private async Task<HttpClient> CreateSignedInMemberClientAsync(
        string email,
        string displayName,
        string subject,
        WebApplicationFactoryClientOptions? options = null) =>
        await CreateSignedInMemberClientAsync(factory, email, displayName, subject, options);

    private async Task<HttpClient> CreateSignedInMemberClientWithLegacyMatchAsync(
        string email,
        string displayName,
        string subject,
        int legacyUserId,
        string legacyUsername,
        WebApplicationFactoryClientOptions? options = null) =>
        await CreateSignedInMemberClientWithLegacyMatchesAsync(
            email,
            displayName,
            subject,
            [new LegacyMemberMatch(legacyUserId, legacyUsername)],
            options);

    private async Task<HttpClient> CreateSignedInMemberClientWithLegacyMatchesAsync(
        string email,
        string displayName,
        string subject,
        IReadOnlyList<LegacyMemberMatch> matches,
        WebApplicationFactoryClientOptions? options = null)
    {
        var specialized = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILegacyMemberLookupRepository>();
                services.AddSingleton<ILegacyMemberLookupRepository>(_ =>
                    new InMemoryLegacyMemberLookupRepository(
                        new Dictionary<string, IReadOnlyList<LegacyMemberMatch>>(StringComparer.OrdinalIgnoreCase)
                        {
                            [email] = matches,
                        }));
            });
        });

        return await CreateSignedInMemberClientAsync(specialized, email, displayName, subject, options);
    }

    private static async Task<HttpClient> CreateSignedInMemberClientAsync(
        WebApplicationFactory<Program> sourceFactory,
        string email,
        string displayName,
        string subject,
        WebApplicationFactoryClientOptions? options = null)
    {
        var client = sourceFactory.CreateClient(options ?? new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = true,
        });
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.ProviderHeader, "Google");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.SubjectHeader, subject);
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.EmailHeader, email);
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.NameHeader, displayName);

        var callbackResponse = await client.GetAsync("/account/external-login-callback");
        Assert.True(
            callbackResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect,
            $"Unexpected callback status code: {callbackResponse.StatusCode}");
        if (callbackResponse.StatusCode == HttpStatusCode.Redirect)
        {
            Assert.DoesNotContain("/account/login", callbackResponse.Headers.Location!.OriginalString);
        }

        return client;
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("""name="__RequestVerificationToken" value="(?<token>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();
}
