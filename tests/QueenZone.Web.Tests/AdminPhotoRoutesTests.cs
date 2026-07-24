using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using QueenZone.Web;
using QueenZone.Web.Pages.Admin.Photos;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace QueenZone.Web.Tests;

public sealed class AdminPhotoRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public AdminPhotoRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task AdminPhotosIndex_RendersSeedPhotos()
    {
        var client = AdminHttpTestHelpers.CreateClient(factory, AdminHttpTestHelpers.AdminEmail);
        var response = await client.GetAsync("/admin/photos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Brian in action with his guitar", body);
        Assert.Contains("href=\"/admin/photos/new\"", body);
    }

    [Fact]
    public async Task AdminPhotos_CreateHideAndShow_RoundTrip()
    {
        var client = AdminHttpTestHelpers.CreateClient(factory, AdminHttpTestHelpers.AdminEmail);

        var newPage = await client.GetStringAsync("/admin/photos/new");
        var token = AdminHttpTestHelpers.ExtractAntiforgeryToken(newPage);

        await using var imageStream = await CreateJpegAsync(320, 240);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(token), AdminPhotosPageModel.AntiforgeryTokenFieldName);
        content.Add(new StringContent("9"), "catId");
        content.Add(new StringContent("Route upload photo"), "title");
        content.Add(new StringContent("2024"), "year");
        content.Add(new StringContent("true"), "isVisible");
        content.Add(new StringContent(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm")), "dateTime");

        var fileContent = new StreamContent(imageStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "route-upload.jpg");

        var createResponse = await client.PostAsync("/admin/photos/create", content);
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var editPath = createResponse.Headers.Location!.OriginalString;
        Assert.StartsWith("/admin/photos/", editPath, StringComparison.Ordinal);

        var picId = int.Parse(editPath.Split('/')[^1], System.Globalization.CultureInfo.InvariantCulture);

        var hideResponse = await PostActionAsync(client, $"/admin/photos/{picId}/hide");
        Assert.Equal(HttpStatusCode.Redirect, hideResponse.StatusCode);

        var publicResponse = await client.GetAsync($"/photography/brian-may/{picId}");
        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);

        var showResponse = await PostActionAsync(client, $"/admin/photos/{picId}/show");
        Assert.Equal(HttpStatusCode.Redirect, showResponse.StatusCode);

        var editPage = await client.GetAsync($"/admin/photos/{picId}");
        Assert.Equal(HttpStatusCode.OK, editPage.StatusCode);
        var editBody = await editPage.Content.ReadAsStringAsync();
        Assert.Contains("Route upload photo", editBody);
        Assert.Contains("Hide from gallery", editBody);
    }

    [Fact]
    public async Task AdminPhotos_RegenerateThumb_SucceedsForUploadedPhoto()
    {
        var client = AdminHttpTestHelpers.CreateClient(factory, AdminHttpTestHelpers.AdminEmail);
        var newPage = await client.GetStringAsync("/admin/photos/new");
        var token = AdminHttpTestHelpers.ExtractAntiforgeryToken(newPage);

        await using var imageStream = await CreateJpegAsync(400, 400);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(token), AdminPhotosPageModel.AntiforgeryTokenFieldName);
        content.Add(new StringContent("9"), "catId");
        content.Add(new StringContent("Thumb regen photo"), "title");
        content.Add(new StringContent("true"), "isVisible");
        var fileContent = new StreamContent(imageStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "thumb-regen.jpg");

        var createResponse = await client.PostAsync("/admin/photos/create", content);
        var picId = int.Parse(
            createResponse.Headers.Location!.OriginalString.Split('/')[^1],
            System.Globalization.CultureInfo.InvariantCulture);

        var regenResponse = await PostActionAsync(client, $"/admin/photos/{picId}/regeneratethumb");
        Assert.Equal(HttpStatusCode.Redirect, regenResponse.StatusCode);

        var editBody = await client.GetStringAsync($"/admin/photos/{picId}");
        Assert.Contains("Thumbnail regenerated", editBody);
    }

    private static async Task<HttpResponseMessage> PostActionAsync(HttpClient client, string actionPath)
    {
        var editPath = string.Join('/', actionPath.Split('/')[..^1]);
        var editPage = await client.GetStringAsync(editPath);
        var fields = new Dictionary<string, string>
        {
            [AdminPhotosPageModel.AntiforgeryTokenFieldName] = AdminHttpTestHelpers.ExtractAntiforgeryToken(editPage),
        };
        return await client.PostAsync(actionPath, new FormUrlEncodedContent(fields));
    }

    private static async Task<MemoryStream> CreateJpegAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        await image.SaveAsJpegAsync(stream, new JpegEncoder { Quality = 80 });
        stream.Position = 0;
        return stream;
    }
}
