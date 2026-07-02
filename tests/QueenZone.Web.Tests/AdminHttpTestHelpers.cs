using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using QueenZone.Web;
using QueenZone.Web.Pages.Admin.News;
using QueenZone.Web.Pages.Admin.NewsDiscovery;

namespace QueenZone.Web.Tests;

internal static partial class AdminHttpTestHelpers
{
    internal const string AdminEmail = "admin@test.local";

    internal static HttpClient CreateClient(WebApplicationFactory<Program> factory, string? email = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        if (!string.IsNullOrWhiteSpace(email))
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, email);
        }

        return client;
    }

    internal static async Task<HttpResponseMessage> PostArticleAsync(
        HttpClient client,
        string formPath,
        string postPath,
        Dictionary<string, string> fields,
        bool includeAntiforgeryToken = true)
    {
        if (includeAntiforgeryToken)
        {
            var formPage = await client.GetStringAsync(formPath);
            fields[AdminNewsPageModel.AntiforgeryTokenFieldName] = ExtractAntiforgeryToken(formPage);
        }

        return await client.PostAsync(postPath, new FormUrlEncodedContent(fields));
    }

    internal static async Task<HttpResponseMessage> PostNewsActionAsync(
        HttpClient client,
        string actionPath,
        bool includeAntiforgeryToken = true)
    {
        var fields = new Dictionary<string, string>();
        if (includeAntiforgeryToken)
        {
            var listPage = await client.GetStringAsync("/admin/news");
            fields[AdminNewsPageModel.AntiforgeryTokenFieldName] = ExtractAntiforgeryToken(listPage);
        }

        return await client.PostAsync(actionPath, new FormUrlEncodedContent(fields));
    }

    internal static async Task<HttpResponseMessage> PostDiscoveryActionAsync(
        HttpClient client,
        string actionPath,
        int candidateId,
        bool includeAntiforgeryToken = true)
    {
        var fields = new Dictionary<string, string>();
        if (includeAntiforgeryToken)
        {
            var reviewPage = await client.GetStringAsync($"/admin/news-discovery/{candidateId}");
            fields[AdminNewsDiscoveryPageModel.AntiforgeryTokenFieldName] = ExtractAntiforgeryToken(reviewPage);
        }

        return await client.PostAsync(actionPath, new FormUrlEncodedContent(fields));
    }

    internal static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }

    internal static int ParseNewsIdFromEditRedirect(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var editPath = response.Headers.Location!.OriginalString;
        return int.Parse(editPath.Split('/')[3], System.Globalization.CultureInfo.InvariantCulture);
    }

    [GeneratedRegex("""name="__RequestVerificationToken" value="(?<token>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();
}
