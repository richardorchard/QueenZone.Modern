namespace QueenZone.Data;

public sealed class ForumOptions
{
    public const string SectionName = "Forum";

    /// <summary>
    /// Minutes after posting during which the author may edit. 0 disables member editing; -1 allows editing indefinitely.
    /// Admins may edit regardless of this value.
    /// </summary>
    public int PostEditWindowMinutes { get; set; } = 60;
}
