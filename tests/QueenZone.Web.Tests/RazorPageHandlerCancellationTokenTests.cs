using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Tests;

public sealed class RazorPageHandlerCancellationTokenTests
{
    private static readonly string[] HandlerVerbs =
    [
        "Get",
        "Post",
        "Put",
        "Delete",
        "Head",
        "Patch",
        "Options",
    ];

    [Fact]
    public void AsyncPageHandlers_AcceptCancellationToken()
    {
        var handlers = GetAsyncPageHandlers().ToList();
        Assert.True(
            handlers.Count >= 190,
            $"Expected at least 190 async Razor page handlers, found {handlers.Count}.");

        var missing = handlers
            .Where(method => !HasCancellationTokenParameter(method))
            .Select(Describe)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "Async Razor page handlers must accept CancellationToken so ASP.NET Core can bind HttpContext.RequestAborted:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, missing));
    }

    [Fact]
    public void AsyncPageHandlers_PlaceCancellationTokenLast()
    {
        var misplaced = GetAsyncPageHandlers()
            .Where(HasCancellationTokenParameter)
            .Where(method => method.GetParameters()[^1].ParameterType != typeof(CancellationToken))
            .Select(Describe)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            misplaced.Count == 0,
            "CancellationToken must be the last Razor page handler parameter:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, misplaced));
    }

    private static IEnumerable<MethodInfo> GetAsyncPageHandlers() =>
        typeof(Program).Assembly.GetTypes()
            .Where(type =>
                type is { IsClass: true, IsGenericTypeDefinition: false }
                && typeof(PageModel).IsAssignableFrom(type)
                && type.Namespace is { } ns
                && ns.StartsWith("QueenZone.Web.Pages", StringComparison.Ordinal))
            .SelectMany(type => type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(IsAsyncPageHandler);

    private static bool IsAsyncPageHandler(MethodInfo method)
    {
        if (!method.Name.StartsWith("On", StringComparison.Ordinal)
            || !method.Name.EndsWith("Async", StringComparison.Ordinal)
            || !typeof(Task).IsAssignableFrom(method.ReturnType))
        {
            return false;
        }

        var middle = method.Name[2..^5];
        return HandlerVerbs.Any(verb =>
            middle.Equals(verb, StringComparison.Ordinal)
            || (middle.StartsWith(verb, StringComparison.Ordinal)
                && middle.Length > verb.Length
                && char.IsUpper(middle[verb.Length])));
    }

    private static bool HasCancellationTokenParameter(MethodInfo method) =>
        method.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken));

    private static string Describe(MethodInfo method)
    {
        var parameters = string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name));
        return $"{method.DeclaringType!.FullName}.{method.Name}({parameters})";
    }
}
