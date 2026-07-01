using QueenZone.Data;

namespace QueenZone.NewsAgent;

public static class NewsDraftAttributionBuilder
{
    public static NewsDraftSourceAttribution Build(
        NewsDraftStructuredResult draft,
        NewsCandidate candidate,
        NewsDiscoverySource source,
        IReadOnlyList<NewsCandidateEvidence> evidence)
    {
        var sourceUrls = new List<string>();
        var sourceNames = new List<string>();

        foreach (var url in draft.SourceUrls)
        {
            AddUnique(sourceUrls, url);
        }

        foreach (var name in draft.SourceNames)
        {
            AddUnique(sourceNames, name);
        }

        AddUnique(sourceUrls, candidate.SourceUrl);
        AddUnique(sourceUrls, candidate.CanonicalUrl);
        foreach (var item in evidence)
        {
            AddUnique(sourceUrls, item.SourceUrl);
            AddUnique(sourceUrls, item.CanonicalUrl);
            AddUnique(sourceNames, item.SourceName);
        }

        AddUnique(sourceNames, source.DisplayName);

        sourceUrls = sourceUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        sourceNames = sourceNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sourceUrls.Count == 0 || sourceNames.Count == 0)
        {
            throw new InvalidOperationException("Draft attribution must include at least one source URL and source name.");
        }

        var attributionText = string.IsNullOrWhiteSpace(draft.AttributionText)
            ? $"Sources: {string.Join(", ", sourceNames)}"
            : draft.AttributionText;

        var sourceNotes = string.IsNullOrWhiteSpace(draft.SourceNotes)
            ? BuildDefaultSourceNotes(source, evidence)
            : draft.SourceNotes;

        var confidenceNotes = BuildConfidenceNotes(draft, source, evidence);

        return new NewsDraftSourceAttribution(
            sourceUrls,
            sourceNames,
            attributionText,
            sourceNotes,
            confidenceNotes);
    }

    private static string BuildDefaultSourceNotes(
        NewsDiscoverySource source,
        IReadOnlyList<NewsCandidateEvidence> evidence)
    {
        var lines = new List<string> { $"{source.DisplayName} ({source.TrustTier})" };
        foreach (var item in evidence.Take(3))
        {
            lines.Add($"{item.SourceName}: {item.CanonicalUrl}");
        }

        return string.Join(" ", lines);
    }

    private static string BuildConfidenceNotes(
        NewsDraftStructuredResult draft,
        NewsDiscoverySource source,
        IReadOnlyList<NewsCandidateEvidence> evidence)
    {
        var notes = new List<string>();
        if (!string.IsNullOrWhiteSpace(draft.ConfidenceNotes))
        {
            notes.Add(draft.ConfidenceNotes);
        }

        var hasPrimaryEvidence = source.TrustTier == NewsDiscoveryTrustTier.Primary
            || evidence.Any(item => item.SourceTrustTier == NewsDiscoveryTrustTier.Primary);

        if (draft.SecondarySourceWarning || (!hasPrimaryEvidence && source.TrustTier == NewsDiscoveryTrustTier.Secondary))
        {
            notes.Add("Editor review recommended: draft relies on secondary or lower-confidence sourcing.");
        }

        return string.Join(" ", notes);
    }

    private static void AddUnique(ICollection<string> values, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            values.Add(value.Trim());
        }
    }
}
