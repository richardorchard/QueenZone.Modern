namespace QueenZone.Data.Entities;

public sealed class TriviaFactEntity
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Difficulty { get; set; }

    public string? Source { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsPublished { get; set; }
}
