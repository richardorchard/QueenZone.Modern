using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed record ArticleFormViewModel(
    string Title,
    string Action,
    AdminNewsDraft Draft,
    IReadOnlyList<string>? Errors,
    AdminNewsArticle? Article = null,
    NewsDiscoveryProvenance? DiscoveryProvenance = null,
    string? Subtitle = null);
