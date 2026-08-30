using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Trivia;

public sealed record TriviaFormViewModel(
    string Title,
    string Action,
    AdminTriviaDraft Draft,
    IReadOnlyList<string>? Errors,
    TriviaFactItem? Fact = null);
