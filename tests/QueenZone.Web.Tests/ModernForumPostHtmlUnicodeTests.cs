using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class ModernForumPostHtmlUnicodeTests
{
    [Fact]
    public void BodyHtml_and_SignatureHtml_are_unicode_nvarchar_max()
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer("Server=localhost;Database=ForumPostUnicodeMetadata;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        using var dbContext = new QueenZoneDbContext(options);
        var entity = dbContext.Model.FindEntityType(typeof(ModernForumPostEntity));
        Assert.NotNull(entity);

        AssertUnicodeMax(entity.FindProperty(nameof(ModernForumPostEntity.BodyHtml)));
        AssertUnicodeMax(entity.FindProperty(nameof(ModernForumPostEntity.SignatureHtml)));

        var title = dbContext.Model
            .FindEntityType(typeof(ModernForumThreadEntity))
            ?.FindProperty(nameof(ModernForumThreadEntity.Title));
        Assert.NotNull(title);
        Assert.Equal("nvarchar(200)", title.GetColumnType());
    }

    private static void AssertUnicodeMax(IProperty? property)
    {
        Assert.NotNull(property);
        Assert.True(property.IsUnicode());
        Assert.Null(property.GetMaxLength());
        Assert.Equal("nvarchar(max)", property.GetColumnType());
    }

    [Fact]
    public void WidenMigration_AltersBodyHtmlAndSignatureHtmlToNvarcharMax()
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer("Server=localhost;Database=MigrationSqlShape;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        using var dbContext = new QueenZoneDbContext(options);
        var migrator = dbContext.Database.GetService<IMigrator>();

        var sql = migrator.GenerateScript(
            "20260831065121_AddAdminOptimisticConcurrencyTokens",
            "20260901104128_WidenModernForumPostHtmlToNvarcharMax");

        Assert.Contains("ALTER TABLE dbo.ModernForumPost", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER COLUMN BodyHtml nvarchar(max) NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER COLUMN SignatureHtml nvarchar(max) NULL", sql, StringComparison.Ordinal);
        Assert.Contains("DROP FULLTEXT INDEX ON dbo.ModernForumPost", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("varchar(8000)", sql, StringComparison.Ordinal);
    }
}
