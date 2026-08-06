namespace QueenZone.Web.E2E;

/// <summary>
/// Deterministic first/last/random sampling for the sitemap sweep (#545), so a failing URL
/// set can be replayed exactly rather than re-rolled on the next run.
/// </summary>
internal static class SeededSampler
{
    public const int Seed = 545;

    /// <summary>
    /// Picks the first item, the last item, and a fixed-seed random selection of the rest,
    /// up to <paramref name="cap"/> items total. Returns <paramref name="items"/> unchanged
    /// if it already fits within the cap.
    /// </summary>
    public static IReadOnlyList<T> SampleFirstLastAndRandom<T>(IReadOnlyList<T> items, int cap)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cap);

        if (items.Count <= cap)
        {
            return items;
        }

        if (cap == 1)
        {
            return [items[0]];
        }

        var picked = new List<T> { items[0] };
        if (items.Count > 1)
        {
            picked.Add(items[^1]);
        }

        var random = new Random(Seed);
        var remainingIndices = Enumerable.Range(1, Math.Max(items.Count - 2, 0)).ToList();
        while (picked.Count < cap && remainingIndices.Count > 0)
        {
            var pick = random.Next(remainingIndices.Count);
            picked.Add(items[remainingIndices[pick]]);
            remainingIndices.RemoveAt(pick);
        }

        return picked;
    }
}
