namespace QueenZone.Data;

public sealed record TriviaFactItem(
    int Id,
    string Text,
    DateTime CreatedAt,
    bool IsPublished,
    string? Category = null,
    string? Difficulty = null,
    string? Source = null);
