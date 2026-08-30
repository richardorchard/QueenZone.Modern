namespace QueenZone.Data;

public sealed record AdminTriviaDraft(
    string Text,
    bool IsPublished,
    string? Category = null,
    string? Difficulty = null,
    string? Source = null);
