using System.Reflection;
using QueenZone.Data;
using QueenZone.Web;
using QueenZone.Web.Pages.Admin.NewsDiscovery;
using SuggestionActionModel = QueenZone.Web.Pages.Admin.NewsSuggestions.ActionModel;

namespace QueenZone.Web.Tests;

public sealed class AdminNewsActionLayeringTests
{
    [Fact]
    public void DiscoveryAndSuggestionActionModels_DoNotTakeServiceProviderOrDbContext()
    {
        AssertConstructorUsesWriteService(typeof(ActionModel), typeof(AdminNewsWriteService));
        AssertConstructorUsesWriteService(typeof(SuggestionActionModel), typeof(NewsSuggestionService));
    }

    [Fact]
    public void PagesSource_DoesNotReferenceDbContextOrEntityFramework()
    {
        var pagesDir = Path.Combine(FindRepoRoot(), "src", "QueenZone.Web", "Pages");
        Assert.True(Directory.Exists(pagesDir), pagesDir);
        var files = Directory.GetFiles(pagesDir, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", text);
            Assert.DoesNotContain("QueenZoneDbContext", text);
            Assert.DoesNotContain("IServiceProvider.GetService", text);
        }
    }

    private static void AssertConstructorUsesWriteService(Type pageModel, Type writeService)
    {
        var ctor = Assert.Single(pageModel.GetConstructors());
        var parameterTypes = ctor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        Assert.DoesNotContain(typeof(IServiceProvider), parameterTypes);
        Assert.DoesNotContain(typeof(QueenZoneDbContext), parameterTypes);
        Assert.Contains(writeService, parameterTypes);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "QueenZone.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find QueenZone.sln from the test output directory.");
    }
}
