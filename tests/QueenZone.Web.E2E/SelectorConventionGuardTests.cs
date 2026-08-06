using System.Runtime.CompilerServices;

namespace QueenZone.Web.E2E;

/// <summary>
/// Fails the PR gate if a Playwright locator reaches for a selector pattern the
/// "Selector conventions" section of docs/architecture/testing-policy.md calls out as
/// fragile — third-party library internals in particular, since a library upgrade can
/// rename or restructure them with no relation to a code change in this repo.
/// Source-scanning (not reflection) because the target is locator string literals, not
/// runtime behavior. Uses <see cref="CallerFilePathAttribute"/> to find this project's
/// source directory regardless of checkout location.
/// </summary>
[TestFixture]
[Category(E2ECategories.Deterministic)]
[Category(E2ECategories.ReadOnly)]
public class SelectorConventionGuardTests
{
    // Add an entry here (and to docs/architecture/testing-policy.md's Selector conventions
    // section) when a new third-party widget needs the same treatment as Quill.
    private static readonly string[] BannedSelectorFragments =
    [
        ".ql-editor", // Quill's own class - tag the element with data-testid instead.
    ];

    private static string ThisFilePath([CallerFilePath] string path = "") => path;

    [Test]
    public void NoTestFileReferencesBannedThirdPartySelectors()
    {
        var thisFilePath = ThisFilePath();
        var directory = Path.GetDirectoryName(thisFilePath);
        Assert.That(directory, Is.Not.Null.And.Not.Empty, "Could not resolve the E2E test source directory.");

        var thisFileName = Path.GetFileName(thisFilePath);
        var sourceFiles = Directory.GetFiles(directory!, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(f => !string.Equals(Path.GetFileName(f), thisFileName, StringComparison.Ordinal))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        Assert.That(sourceFiles, Is.Not.Empty, "Expected to find other .cs files alongside this guard test.");

        var violations = new List<string>();
        foreach (var file in sourceFiles)
        {
            var lines = File.ReadAllLines(file);
            for (var lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                foreach (var banned in BannedSelectorFragments)
                {
                    if (lines[lineNumber].Contains(banned, StringComparison.Ordinal))
                    {
                        violations.Add($"{Path.GetFileName(file)}:{lineNumber + 1}: banned selector '{banned}'");
                    }
                }
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Fragile selector(s) found (see docs/architecture/testing-policy.md, " +
            "'Selector conventions'): " + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }
}
