using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class QueenZoneHostFilteringOptions
{
    public const string SectionName = "QueenZoneHostFiltering";

    public string AllowedHosts { get; set; } = string.Empty;
}

public sealed class QueenZoneHostFilteringOptionsValidator : IValidateOptions<QueenZoneHostFilteringOptions>
{
    public ValidateOptionsResult Validate(string? name, QueenZoneHostFilteringOptions options) =>
        string.IsNullOrWhiteSpace(options.AllowedHosts)
            ? ValidateOptionsResult.Fail($"{QueenZoneHostFilteringOptions.SectionName}:AllowedHosts must not be empty.")
            : ValidateOptionsResult.Success;
}

public sealed class QueenZoneHostFilteringMiddleware
{
    private readonly RequestDelegate next;
    private readonly string[] allowedHosts;

    public QueenZoneHostFilteringMiddleware(
        RequestDelegate next,
        IOptions<QueenZoneHostFilteringOptions> options)
    {
        this.next = next;
        allowedHosts = options.Value.AllowedHosts
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsAllowed(context.Request.Host.Host, allowedHosts))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await next(context);
    }

    internal static bool IsAllowed(string host, IReadOnlyList<string> allowedHosts)
    {
        var normalizedHost = host.TrimEnd('.');
        if (normalizedHost.Length == 0)
        {
            return false;
        }

        foreach (var configuredHost in allowedHosts)
        {
            var pattern = configuredHost.TrimEnd('.');
            if (pattern == "*")
            {
                return true;
            }

            if (pattern.StartsWith("*.", StringComparison.Ordinal))
            {
                var suffix = pattern[1..];
                if (normalizedHost.Length > suffix.Length
                    && normalizedHost.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                continue;
            }

            if (string.Equals(normalizedHost, pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public static class QueenZoneHostFilteringExtensions
{
    public static IApplicationBuilder UseQueenZoneHostFiltering(this IApplicationBuilder app) =>
        app.UseMiddleware<QueenZoneHostFilteringMiddleware>();
}
