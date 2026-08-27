using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Pages.Admin.NewsDiscovery;

public sealed class PromptSettingsModel(
    INewsAgentGuidanceRepository guidanceRepository,
    INewsAgentGuidanceProvider guidanceProvider) : AdminNewsDiscoveryPageModel
{
    public NewsAgentGuidanceSectionViewModel Triage { get; private set; } = NewsAgentGuidanceSectionViewModel.Empty(NewsAgentGuidanceType.Triage);

    public NewsAgentGuidanceSectionViewModel Draft { get; private set; } = NewsAgentGuidanceSectionViewModel.Empty(NewsAgentGuidanceType.Draft);

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public IReadOnlyList<string> Errors { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        StatusMessage = TempData["GuidanceMessage"] as string;
        StatusMessageKind = TempData["GuidanceMessageKind"] as string;
        ViewData["Title"] = "News agent editorial guidance";
        return Page();
    }

    public Task<IActionResult> OnPostSaveDraftAsync(
        NewsAgentGuidanceType type,
        string? content,
        string? rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            type,
            confirmRequired: false,
            confirmed: true,
            rowVersion,
            (expected, ct) => guidanceRepository.SaveDraftAsync(type, content ?? string.Empty, EditorEmail, expected, ct),
            "Draft saved. Publish it to apply the overlay to future runs.",
            cancellationToken);

    public Task<IActionResult> OnPostPublishAsync(
        NewsAgentGuidanceType type,
        string? rowVersion,
        bool confirm,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            type,
            confirmRequired: true,
            confirmed: confirm,
            rowVersion,
            (expected, ct) => guidanceRepository.PublishDraftAsync(
                type,
                EditorEmail,
                expected ?? throw new NewsAgentGuidanceConcurrencyException(),
                ct),
            "Published. Future triage and draft runs use this overlay within 60 seconds. Existing candidates are unchanged.",
            cancellationToken);

    public Task<IActionResult> OnPostRollbackAsync(
        NewsAgentGuidanceType type,
        int revisionId,
        bool confirm,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            type,
            confirmRequired: true,
            confirmed: confirm,
            rowVersion: null,
            (_, ct) => guidanceRepository.RollbackAsync(type, revisionId, EditorEmail, ct),
            "Rolled back by publishing a new revision. History was kept. Future runs pick this up within 60 seconds.",
            cancellationToken);

    public Task<IActionResult> OnPostRestoreDefaultAsync(
        NewsAgentGuidanceType type,
        bool confirm,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            type,
            confirmRequired: true,
            confirmed: confirm,
            rowVersion: null,
            (_, ct) => guidanceRepository.RestoreCompiledDefaultAsync(type, EditorEmail, ct),
            "Compiled default restored. Future runs use the code-controlled prompt with no overlay.",
            cancellationToken);

    private async Task<IActionResult> ExecuteAsync(
        NewsAgentGuidanceType type,
        bool confirmRequired,
        bool confirmed,
        string? rowVersion,
        Func<byte[]?, CancellationToken, Task<NewsAgentGuidanceRevision>> action,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (confirmRequired && !confirmed)
        {
            await LoadAsync(cancellationToken);
            Errors = ["Confirm this action before continuing."];
            ViewData["Title"] = "News agent editorial guidance";
            return Page();
        }

        try
        {
            await action(DecodeRowVersion(rowVersion), cancellationToken);
            guidanceProvider.Invalidate(type);
            TempData["GuidanceMessage"] = successMessage;
            TempData["GuidanceMessageKind"] = "success";
            return RedirectToPage();
        }
        catch (NewsAgentGuidanceConcurrencyException ex)
        {
            await LoadAsync(cancellationToken);
            Errors = [ex.Message];
            ViewData["Title"] = "News agent editorial guidance";
            return Page();
        }
        catch (NewsAgentGuidanceValidationException ex)
        {
            await LoadAsync(cancellationToken);
            Errors = [ex.Message];
            ViewData["Title"] = "News agent editorial guidance";
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            await LoadAsync(cancellationToken);
            Errors = [ex.Message];
            ViewData["Title"] = "News agent editorial guidance";
            return Page();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Triage = await LoadSectionAsync(NewsAgentGuidanceType.Triage, cancellationToken);
        Draft = await LoadSectionAsync(NewsAgentGuidanceType.Draft, cancellationToken);
    }

    private async Task<NewsAgentGuidanceSectionViewModel> LoadSectionAsync(
        NewsAgentGuidanceType type,
        CancellationToken cancellationToken)
    {
        var published = await guidanceRepository.GetPublishedAsync(type, cancellationToken);
        var draft = await guidanceRepository.GetDraftAsync(type, cancellationToken);
        var history = await guidanceRepository.ListHistoryAsync(type, cancellationToken);
        var compiledPrompt = type == NewsAgentGuidanceType.Triage
            ? NewsTriagePrompt.BuildCompiledSystemPrompt()
            : NewsDraftPrompt.BuildCompiledSystemPrompt();
        var previewGuidance = draft?.Content ?? published?.Content;
        var composedPreview = type == NewsAgentGuidanceType.Triage
            ? NewsTriagePrompt.ComposeSystemPrompt(previewGuidance)
            : NewsDraftPrompt.ComposeSystemPrompt(previewGuidance);

        return new NewsAgentGuidanceSectionViewModel(
            type,
            NewsAgentGuidanceText.ToStorageType(type),
            published,
            draft,
            history,
            published is null || string.IsNullOrWhiteSpace(published.Content),
            compiledPrompt,
            composedPreview,
            NewsAgentGuidanceDiff.Compare(published?.Content, draft?.Content));
    }

    private static byte[]? DecodeRowVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new NewsAgentGuidanceConcurrencyException();
        }
    }
}

public sealed record NewsAgentGuidanceSectionViewModel(
    NewsAgentGuidanceType Type,
    string TypeKey,
    NewsAgentGuidanceRevision? Published,
    NewsAgentGuidanceRevision? Draft,
    IReadOnlyList<NewsAgentGuidanceRevision> History,
    bool UsingCompiledDefault,
    string CompiledSystemPrompt,
    string ComposedPreview,
    IReadOnlyList<NewsAgentGuidanceDiffLine> Diff)
{
    public static NewsAgentGuidanceSectionViewModel Empty(NewsAgentGuidanceType type) =>
        new(
            type,
            NewsAgentGuidanceText.ToStorageType(type),
            null,
            null,
            [],
            true,
            string.Empty,
            string.Empty,
            []);

    public string DraftContent => Draft?.Content ?? Published?.Content ?? string.Empty;

    public string DraftRowVersion =>
        Draft is null ? string.Empty : Convert.ToBase64String(Draft.RowVersion);

    public string PublishedSummary
    {
        get
        {
            if (Published is null)
            {
                return "No published revision. The worker uses the compiled default.";
            }

            var publisher = Published.PublishedByEmail ?? Published.CreatedByEmail;
            var publishedAt = Published.PublishedAt?.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture) ?? "unknown time";
            return $"Revision {Published.RevisionNumber} published by {publisher} at {publishedAt} UTC.";
        }
    }
}
