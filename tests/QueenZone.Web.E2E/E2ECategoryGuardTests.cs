using System.Reflection;

namespace QueenZone.Web.E2E;

/// <summary>
/// Ensures every concrete Playwright fixture is tagged so CI filters cannot silently
/// drop new tests into neither the PR gate nor the nightly RealData suite.
/// </summary>
[TestFixture]
[Category(E2ECategories.Deterministic)]
[Category(E2ECategories.ReadOnly)]
public class E2ECategoryGuardTests
{
    private static readonly string[] SuiteCategories =
    [
        E2ECategories.Deterministic,
        E2ECategories.RealData
    ];

    [Test]
    public void EveryConcreteFixtureHasDeterministicOrRealDataCategory()
    {
        var assembly = typeof(E2EPageTest).Assembly;
        var fixtures = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true })
            .Where(IsNUnitFixture)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        Assert.That(fixtures, Is.Not.Empty, "Expected at least one NUnit fixture in QueenZone.Web.E2E.");

        var missing = fixtures
            .Where(t => !HasAnySuiteCategory(t))
            .Select(t => t.Name)
            .ToList();

        Assert.That(
            missing,
            Is.Empty,
            "These fixtures need [Category(\"Deterministic\")] or [Category(\"RealData\")] " +
            "(and optionally [Category(\"ReadOnly\")]): " + string.Join(", ", missing));
    }

    [Test]
    public void DeterministicFixturesArePresent()
    {
        var assembly = typeof(E2EPageTest).Assembly;
        var deterministic = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true })
            .Where(IsNUnitFixture)
            .Where(t => HasCategory(t, E2ECategories.Deterministic))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        // Keep this list in sync when adding Deterministic fixtures so an accidental
        // category rename or drop is visible in the PR gate.
        var expected = new[]
        {
            nameof(AccessibilitySmokeTests),
            nameof(AdminSmokeTests),
            nameof(E2ECategoryGuardTests),
            nameof(EditorWorkflowTests),
            nameof(ForumPostingWorkflowTests),
            nameof(RealDataMarkerTests),
            nameof(SmokeTests),
        };

        Assert.That(deterministic, Is.EqualTo(expected));
    }

    [Test]
    public void RealDataFixturesArePresent()
    {
        // Acceptance for #545 (phase 2 of #543): --filter TestCategory=RealData now selects
        // the sitemap sweep. Keep this list in sync as further phase-2 issues (#546-#548) land.
        var assembly = typeof(E2EPageTest).Assembly;
        var realData = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true })
            .Where(IsNUnitFixture)
            .Where(t => HasCategory(t, E2ECategories.RealData))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var expected = new[]
        {
            nameof(SitemapPublicRouteSweepTests),
        };

        Assert.That(realData, Is.EqualTo(expected));
    }


    private static bool IsNUnitFixture(Type type)
    {
        if (type.GetCustomAttributes(typeof(TestFixtureAttribute), inherit: true).Length > 0)
        {
            return true;
        }

        return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Any(m => m.GetCustomAttributes(typeof(TestAttribute), inherit: true).Length > 0);
    }

    private static bool HasAnySuiteCategory(Type type) =>
        SuiteCategories.Any(c => HasCategory(type, c));

    private static bool HasCategory(Type type, string category) =>
        type.GetCustomAttributes(typeof(CategoryAttribute), inherit: true)
            .OfType<CategoryAttribute>()
            .Any(a => string.Equals(a.Name, category, StringComparison.Ordinal));
}
