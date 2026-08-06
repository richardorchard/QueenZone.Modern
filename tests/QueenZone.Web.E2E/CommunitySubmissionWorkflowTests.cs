using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using QueenZone.Data.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace QueenZone.Web.E2E;

/// <summary>
/// Member-facing community content submission journeys (#546): photo, article, and news
/// suggestion submission through the real forms, confirmation pages, and
/// <c>/account/my-submissions</c> status, plus account settings display-name updates and
/// per-form validation. Runs against the SQL Express mirror (<c>ASPNETCORE_ENVIRONMENT=E2E</c>);
/// every row this fixture creates is tagged with the <c>uie2e-{runId}-...</c> marker convention
/// and deleted in <see cref="CleanupCreatedRowsAsync"/>. The admin moderation half of the
/// pipeline is a separate issue (#540 sibling) and is out of scope here.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category(E2ECategories.RealData)]
public class CommunitySubmissionWorkflowTests : RealDataPageTest
{
    private const string MemberIdHeader = "X-Test-Member-Id";
    private const string MemberNameHeader = "X-Test-Member-Name";
    private const string MemberEmailHeader = "X-Test-Member-Email";
    private const string AdminEmailHeader = "X-Test-User-Email";

    private static string AdminEmail =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@test.local";

    [Test]
    public async Task Member_can_submit_photo_and_see_it_pending_in_my_submissions()
    {
        var member = await CreateMemberAsync("photo-submit");
        var title = $"{member.Marker} photo title";

        await Page.GotoAsync("/submit/photo");
        await Page.GetByLabel("Title").FillAsync(title);
        await Page.GetByLabel("Description").FillAsync("Disposable E2E photo submission.");
        await Page.GetByLabel("Suggested category").FillAsync("Live");
        await Page.GetByLabel("Photo").SetInputFilesAsync(new FilePayload
        {
            Name = "e2e-photo.png",
            MimeType = "image/png",
            Buffer = GeneratePngBytes(400, 300),
        });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit for review" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(".*/submit/photo/confirmation/.+"));
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Your photo is under review.");
        await Expect(Page.Locator(".qz-account-settings__meta")).ToContainTextAsync(title);
        await Expect(Page.Locator(".qz-account-settings__meta")).ToContainTextAsync("Pending");

        await Page.GotoAsync("/account/my-submissions");
        var row = Page.Locator("table.admin-table tbody tr").Filter(new() { HasText = title });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row.GetByText("Pending")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Member_can_submit_article_with_cover_image_and_see_it_pending()
    {
        var member = await CreateMemberAsync("article-submit");
        var title = $"{member.Marker} article title";

        await Page.GotoAsync("/submit/article");
        await Page.GetByLabel("Title").FillAsync(title);
        await Page.GetByLabel("Excerpt").FillAsync("Disposable E2E article submission excerpt.");
        await FillRichTextEditorAsync(BuildArticleBody(member.Marker));

        var fileChooser = await Page.RunAndWaitForFileChooserAsync(async () =>
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = "Attach file" }).ClickAsync();
        });
        await fileChooser.SetFilesAsync(new FilePayload
        {
            Name = "e2e-cover.png",
            MimeType = "image/png",
            Buffer = GeneratePngBytes(600, 400),
        });
        await Expect(Page.Locator(".qz-rte-progress")).ToBeHiddenAsync();
        await Expect(Page.Locator(".ql-editor img")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit for review" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(".*/submit/article/confirmation/.+"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = title, Level = 2 })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Status: Submitted")).ToBeVisibleAsync();

        await Page.GotoAsync("/account/my-submissions?tab=articles");
        var row = Page.Locator("table.admin-table tbody tr").Filter(new() { HasText = title });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row.GetByText("Submitted")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Member_can_suggest_news_and_it_reaches_admin_queue_as_pending()
    {
        var member = await CreateMemberAsync("news-suggest");
        var storyUrl = $"https://example.com/{member.Marker}-story";
        var headline = $"{member.Marker} suggested headline";

        await Page.GotoAsync("/submit/news");
        await Page.GetByLabel("News story URL").FillAsync(storyUrl);
        await Page.GetByLabel("Suggested headline").FillAsync(headline);
        await Page.GetByLabel("Notes for the editor").FillAsync("Disposable E2E news suggestion.");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit suggestion" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(".*/submit/news/confirmation"));
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Thank you for the suggestion!");

        var adminContext = await CreateExtraContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            ExtraHTTPHeaders = new Dictionary<string, string> { [AdminEmailHeader] = AdminEmail },
        });
        var adminPage = await adminContext.NewPageAsync();
        await adminPage.GotoAsync("/admin/news-suggestions");
        var row = adminPage.Locator("table.admin-table tbody tr").Filter(new() { HasText = headline });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row.GetByText("Pending")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Member_can_update_display_name_and_it_persists_across_a_subsequent_submission()
    {
        var member = await CreateMemberAsync("account-settings");
        var newName = $"E2E Renamed {member.Marker}";

        await Page.GotoAsync("/account/settings");
        await Page.GetByLabel("Display name").FillAsync(newName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save display name" }).ClickAsync();

        await Expect(Page.GetByText("Display name updated.")).ToBeVisibleAsync();
        await Expect(Page.Locator(".qz-masthead__member-name").First).ToHaveTextAsync(newName);

        var photoTitle = $"{member.Marker} attribution photo";
        await Page.GotoAsync("/submit/photo");
        await Page.GetByLabel("Title").FillAsync(photoTitle);
        await Page.GetByLabel("Photo").SetInputFilesAsync(new FilePayload
        {
            Name = "e2e-photo.png",
            MimeType = "image/png",
            Buffer = GeneratePngBytes(300, 200),
        });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit for review" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(".*/submit/photo/confirmation/.+"));
        await Expect(Page.Locator(".qz-masthead__member-name").First).ToHaveTextAsync(newName);
    }

    [Test]
    public async Task Photo_submission_missing_title_shows_required_validation_message()
    {
        // Photo.cshtml's form has data-busy-submit, whose site.js handler calls
        // form.reportValidity() on submit regardless of noValidate, so the JS bypass used for
        // Article/News does not reach the server here. A single space satisfies the native
        // "required" constraint (non-empty) but still fails [Required] server-side, which trims
        // before checking — the same server round trip, without fighting the busy-submit script.
        await CreateMemberAsync("photo-validate-required");
        await Page.GotoAsync("/submit/photo");
        await Page.GetByLabel("Title").FillAsync(" ");
        await Page.GetByLabel("Photo").SetInputFilesAsync(new FilePayload
        {
            Name = "e2e-photo.png",
            MimeType = "image/png",
            Buffer = GeneratePngBytes(200, 200),
        });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit for review" }).ClickAsync();

        await Expect(Page.GetByText("Title is required.").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task Photo_submission_wrong_file_type_shows_validation_message()
    {
        var member = await CreateMemberAsync("photo-validate-type");
        var title = $"{member.Marker} wrong type photo";

        await Page.GotoAsync("/submit/photo");
        await Page.GetByLabel("Title").FillAsync(title);
        await Page.GetByLabel("Photo").SetInputFilesAsync(new FilePayload
        {
            Name = "e2e-not-an-image.txt",
            MimeType = "text/plain",
            Buffer = Encoding.UTF8.GetBytes("This is not an image."),
        });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit for review" }).ClickAsync();

        await Expect(Page.GetByText("Photo must be a JPEG, PNG, WebP, or TIFF image.")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Article_submission_missing_title_shows_required_validation_message()
    {
        var member = await CreateMemberAsync("article-validate-required");
        await Page.GotoAsync("/submit/article");
        await DisableNativeValidationAsync();
        await FillRichTextEditorAsync(BuildArticleBody(member.Marker));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit for review" }).ClickAsync();

        await Expect(Page.GetByText("Title is required.").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task News_suggestion_missing_story_url_shows_required_validation_message()
    {
        await CreateMemberAsync("news-validate-required");
        await Page.GotoAsync("/submit/news");
        await DisableNativeValidationAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit suggestion" }).ClickAsync();

        await Expect(Page.GetByText("URL is required.").First).ToBeVisibleAsync();
    }

    /// <summary>
    /// Seeds a real <c>MemberAccounts</c> row (submission forms look the member up via
    /// <c>MemberAccountService</c>/repositories, so a bare <c>X-Test-Member-Id</c> claim with no
    /// backing row is not enough) and points the browser context's impersonation headers at it.
    /// </summary>
    private async Task<MemberContext> CreateMemberAsync(string fixtureSlug)
    {
        var marker = NextMarker(fixtureSlug);
        var memberId = Guid.NewGuid();
        var email = $"{marker}@e2e.queenzone.local";
        var displayName = $"E2E {marker}";

        await using (var db = RealDataDb.CreateContext())
        {
            db.MemberAccounts.Add(new MemberAccount
            {
                Id = memberId,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                DisplayName = displayName,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        await Context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            [MemberIdHeader] = memberId.ToString(),
            [MemberNameHeader] = displayName,
            [MemberEmailHeader] = email,
        });

        return new MemberContext(memberId, marker, displayName, email);
    }

    protected override async Task CleanupCreatedRowsAsync(IReadOnlyList<string> markers)
    {
        await using var db = RealDataDb.CreateContext();
        foreach (var marker in markers)
        {
            // Submission rows first — MemberAccounts is a Restrict FK target for all three.
            await db.PhotoSubmissions.Where(s => s.Title.Contains(marker)).ExecuteDeleteAsync();
            await db.ArticleSubmissions.Where(s => s.Title.Contains(marker)).ExecuteDeleteAsync();
            await db.NewsSuggestions
                .Where(s => s.Url.Contains(marker) || (s.Title != null && s.Title.Contains(marker)))
                .ExecuteDeleteAsync();
            await db.MemberAccounts.Where(m => m.Email.Contains(marker)).ExecuteDeleteAsync();
        }
    }

    private async Task FillRichTextEditorAsync(string text)
    {
        var editor = Page.Locator(".ql-editor").Last;
        await Expect(editor).ToBeVisibleAsync();
        await editor.ClickAsync();
        await Page.Keyboard.InsertTextAsync(text);
    }

    private async Task DisableNativeValidationAsync() =>
        await Page.EvalOnSelectorAsync(
            "form.qz-account-form",
            "form => { form.noValidate = true; }");

    private static string BuildArticleBody(string marker)
    {
        const int minVisibleChars = 320;
        var sentence = $"Disposable E2E article body for marker {marker}. ";
        var builder = new StringBuilder();
        while (builder.Length < minVisibleChars)
        {
            builder.Append(sentence);
        }

        return builder.ToString();
    }

    private static byte[] GeneratePngBytes(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(100, 149, 237, 255));
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    private sealed record MemberContext(Guid Id, string Marker, string DisplayName, string Email);
}
