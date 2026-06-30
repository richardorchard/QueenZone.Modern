using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed partial class MemberAccountFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public MemberAccountFlowTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task RegisterThenSignIn_GrantsAccessToMemberProbe()
    {
        var client = CreateClient();

        var registerResponse = await PostFormAsync(client, "/account/register", "/account/register", new Dictionary<string, string>
        {
            ["email"] = "newmember@example.com",
            ["password"] = "S3curePassword!",
            ["displayName"] = "New Member",
            ["returnUrl"] = "/",
        });
        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);

        var probeResponse = await client.GetAsync("/account/member-probe");
        Assert.Equal(HttpStatusCode.OK, probeResponse.StatusCode);
    }

    [Fact]
    public async Task SiteHeader_ShowsDisplayNameAndSignOut_AfterRegistering()
    {
        var client = CreateClient();

        var homeBeforeSignIn = await client.GetStringAsync("/");
        Assert.Contains("Sign in", homeBeforeSignIn);
        Assert.DoesNotContain("Sign out", homeBeforeSignIn);

        await PostFormAsync(client, "/account/register", "/account/register", new Dictionary<string, string>
        {
            ["email"] = "headerstate@example.com",
            ["password"] = "S3curePassword!",
            ["displayName"] = "Header Tester",
            ["returnUrl"] = "/",
        });

        var homeAfterSignIn = await client.GetStringAsync("/");
        Assert.Contains("Header Tester", homeAfterSignIn);
        Assert.Contains("Sign out", homeAfterSignIn);
    }

    [Fact]
    public async Task Register_RejectsDuplicateEmail()
    {
        var client = CreateClient();

        var firstResponse = await PostFormAsync(client, "/account/register", "/account/register", new Dictionary<string, string>
        {
            ["email"] = "duplicate@example.com",
            ["password"] = "S3curePassword!",
            ["displayName"] = "Member",
            ["returnUrl"] = "/",
        });
        Assert.Equal(HttpStatusCode.Redirect, firstResponse.StatusCode);

        var secondResponse = await PostFormAsync(client, "/account/register", "/account/register", new Dictionary<string, string>
        {
            ["email"] = "duplicate@example.com",
            ["password"] = "DifferentPassword!",
            ["displayName"] = "Member Two",
            ["returnUrl"] = "/",
        });

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var body = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("already exists", body);
    }

    [Fact]
    public async Task SignIn_RejectsWrongPassword()
    {
        var client = CreateClient();
        await PostFormAsync(client, "/account/register", "/account/register", new Dictionary<string, string>
        {
            ["email"] = "signintest@example.com",
            ["password"] = "S3curePassword!",
            ["displayName"] = "Member",
            ["returnUrl"] = "/",
        });

        var freshClient = CreateClient();
        var signInResponse = await PostFormAsync(freshClient, "/account/sign-in", "/account/sign-in", new Dictionary<string, string>
        {
            ["email"] = "signintest@example.com",
            ["password"] = "WrongPassword!",
            ["returnUrl"] = "/",
        });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
        var body = await signInResponse.Content.ReadAsStringAsync();
        Assert.Contains("Incorrect email or password", body);
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string formPath,
        string postPath,
        Dictionary<string, string> fields)
    {
        var formPage = await client.GetStringAsync(formPath);
        fields["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage);
        return await client.PostAsync(postPath, new FormUrlEncodedContent(fields));
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();
}
