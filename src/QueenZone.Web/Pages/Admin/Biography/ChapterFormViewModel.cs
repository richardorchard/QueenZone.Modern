using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Biography;

public sealed record ChapterFormViewModel(
    string Title,
    string Action,
    AdminBiographyDraft Draft,
    IReadOnlyList<string>? Errors,
    BiographyChapterItem? Chapter = null,
    string? Subtitle = null);
