namespace QueenZone.Web;

/// <summary>
/// Named predicates over <see cref="IHostEnvironment.EnvironmentName"/>, so that adding a new
/// automated-test environment (for example <see cref="E2E"/>) is a change in this file instead
/// of every call site that previously compared against an environment name string literal.
/// </summary>
public static class QueenZoneEnvironments
{
    /// <summary>WebApplicationFactory suites: in-memory data/storage, test auth handlers.</summary>
    public const string Testing = "Testing";

    /// <summary>Nightly Playwright suite against the real SQL mirror: test auth handlers, but real data.</summary>
    public const string E2E = "E2E";

    /// <summary>
    /// True when the host should register <see cref="TestAuthHandler"/> and
    /// <see cref="TestMemberAuthHandler"/>, add the member-policy test scheme, and skip the
    /// Entra fail-closed check.
    /// </summary>
    public static bool UsesTestAuth(IHostEnvironment environment) =>
        environment.IsEnvironment(Testing) || environment.IsEnvironment(E2E);

    /// <summary>
    /// True only for the WebApplicationFactory <see cref="Testing"/> host, which uses in-memory
    /// repositories and in-memory blob storage instead of the legacy SQL/blob backends. The
    /// nightly E2E suite needs the real SQL mirror, so it must not match this predicate.
    /// </summary>
    public static bool UsesInMemoryData(IHostEnvironment environment) =>
        environment.IsEnvironment(Testing);

    /// <summary>
    /// True for hosts running as an automated test suite (WebApplicationFactory or the nightly
    /// Playwright E2E run), where output caching, telemetry, response compression, and the
    /// production HSTS/exception-handler branch should be skipped.
    /// </summary>
    public static bool IsAutomatedTestHost(IHostEnvironment environment) =>
        environment.IsEnvironment(Testing) || environment.IsEnvironment(E2E);
}
