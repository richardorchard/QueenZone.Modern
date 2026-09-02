namespace QueenZone.Data;

public sealed record TriviaFactCsvImportRow(
    string Text,
    string? Category,
    string? Difficulty,
    string? Source);
