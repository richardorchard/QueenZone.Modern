using QueenZone.Tools;

namespace QueenZone.Tools.Tests;

public sealed class CreateReviewerAccountCommandTests
{
    [Fact]
    public void Parse_WithAllRequiredArguments_Succeeds()
    {
        var options = CreateReviewerAccountOptions.Parse([
            "create-reviewer-account",
            "--email", "reviewer@example.com",
            "--password", "a-long-enough-password",
            "--display-name", "App Reviewer",
            "--connection-string", "Server=.;Database=QueenZone;",
        ]);

        Assert.True(options.IsValid);
        Assert.Equal("reviewer@example.com", options.Email);
        Assert.Equal("a-long-enough-password", options.Password);
        Assert.Equal("App Reviewer", options.DisplayName);
        Assert.Equal("Server=.;Database=QueenZone;", options.ConnectionString);
    }

    [Fact]
    public void Parse_WithoutDisplayName_DefaultsToAppReviewer()
    {
        var options = CreateReviewerAccountOptions.Parse([
            "create-reviewer-account",
            "--email", "reviewer@example.com",
            "--password", "a-long-enough-password",
            "--connection-string", "Server=.;Database=QueenZone;",
        ]);

        Assert.True(options.IsValid);
        Assert.Equal("App Reviewer", options.DisplayName);
    }

    [Fact]
    public void Parse_WithoutEmail_IsInvalid()
    {
        var options = CreateReviewerAccountOptions.Parse([
            "create-reviewer-account",
            "--password", "a-long-enough-password",
            "--connection-string", "Server=.;Database=QueenZone;",
        ]);

        Assert.False(options.IsValid);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    public void Parse_WithTooShortPassword_IsInvalid(string password)
    {
        var options = CreateReviewerAccountOptions.Parse([
            "create-reviewer-account",
            "--email", "reviewer@example.com",
            "--password", password,
            "--connection-string", "Server=.;Database=QueenZone;",
        ]);

        Assert.False(options.IsValid);
    }

    [Fact]
    public void Parse_WithoutConnectionString_IsInvalid()
    {
        var originalEnv = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy");
        Environment.SetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy", null);
        try
        {
            var options = CreateReviewerAccountOptions.Parse([
                "create-reviewer-account",
                "--email", "reviewer@example.com",
                "--password", "a-long-enough-password",
            ]);

            Assert.False(options.IsValid);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy", originalEnv);
        }
    }

    [Fact]
    public async Task ToolsApp_default_usage_includes_create_reviewer_account_command()
    {
        using var error = new StringWriter();
        var originalError = Console.Error;
        Console.SetError(error);
        try
        {
            var exitCode = await ToolsApp.RunAsync([]);

            Assert.Equal(2, exitCode);
            Assert.Contains("create-reviewer-account", error.ToString());
        }
        finally
        {
            Console.SetError(originalError);
        }
    }
}
