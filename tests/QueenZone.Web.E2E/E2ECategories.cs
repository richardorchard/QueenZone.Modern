namespace QueenZone.Web.E2E;

/// <summary>
/// NUnit <see cref="CategoryAttribute"/> names for the Playwright suite split.
/// <list type="bullet">
/// <item><see cref="Deterministic"/> — PR-gate fixtures against in-memory <c>Testing</c> sample data.</item>
/// <item><see cref="RealData"/> — nightly fixtures against the SQL Express mirror (E2E environment).</item>
/// <item><see cref="ReadOnly"/> — orthogonal marker: fixture performs no database writes (live-site sweep).</item>
/// </list>
/// Every concrete fixture must have either <see cref="Deterministic"/> or <see cref="RealData"/>
/// (see <c>E2ECategoryGuardTests</c>).
/// </summary>
internal static class E2ECategories
{
    public const string Deterministic = "Deterministic";
    public const string RealData = "RealData";
    public const string ReadOnly = "ReadOnly";
}
