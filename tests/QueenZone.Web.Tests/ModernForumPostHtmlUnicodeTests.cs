using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
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
        var assembly = dbContext.GetService<IMigrationsAssembly>();
        Assert.True(assembly.Migrations.TryGetValue(
            "20260901104128_WidenModernForumPostHtmlToNvarcharMax",
            out var migrationType));
        var migration = assembly.CreateMigration(migrationType, dbContext.Database.ProviderName!);
        var up = migration.UpOperations.OfType<SqlOperation>().ToList();

        Assert.Equal(4, up.Count);
        Assert.All(up, operation => Assert.True(operation.SuppressTransaction));

        Assert.Contains("CREATE FULLTEXT INDEX ON dbo.ModernForumPost (BodyHtml)", up[0].Sql, StringComparison.Ordinal);
        Assert.Contains("KEY INDEX PK_ModernForumPost", up[0].Sql, StringComparison.Ordinal);
        Assert.Contains("ON FT_ForumCatalog", up[0].Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("t.name = N'varchar'", up[0].Sql, StringComparison.Ordinal);

        Assert.Contains("DROP FULLTEXT INDEX ON dbo.ModernForumPost", up[1].Sql, StringComparison.Ordinal);
        Assert.Contains("t.name = N'varchar'", up[1].Sql, StringComparison.Ordinal);

        Assert.Contains("ALTER COLUMN BodyHtml nvarchar(max) NOT NULL", up[2].Sql, StringComparison.Ordinal);
        Assert.Contains("ALTER COLUMN SignatureHtml nvarchar(max) NULL", up[2].Sql, StringComparison.Ordinal);
        Assert.Contains("t.name = N'varchar'", up[2].Sql, StringComparison.Ordinal);

        Assert.Contains("CREATE FULLTEXT INDEX ON dbo.ModernForumPost (BodyHtml)", up[3].Sql, StringComparison.Ordinal);
        Assert.Equal(up[0].Sql, up[3].Sql);

        var migrator = dbContext.Database.GetService<IMigrator>();
        var sql = migrator.GenerateScript(
            "20260831065121_AddAdminOptimisticConcurrencyTokens",
            "20260901104128_WidenModernForumPostHtmlToNvarcharMax");

        Assert.Contains("ALTER TABLE dbo.ModernForumPost", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER COLUMN BodyHtml nvarchar(max) NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER COLUMN SignatureHtml nvarchar(max) NULL", sql, StringComparison.Ordinal);
        Assert.Contains("DROP FULLTEXT INDEX ON dbo.ModernForumPost", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN TRANSACTION", ExtractWidenMigrationBody(sql), StringComparison.Ordinal);
        Assert.DoesNotContain("varchar(8000)", sql, StringComparison.Ordinal);
    }

    private static string ExtractWidenMigrationBody(string script)
    {
        const string start = "20260901104128_WidenModernForumPostHtmlToNvarcharMax";
        var from = script.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0);
        var history = script.IndexOf("INSERT INTO [__EFMigrationsHistory]", from, StringComparison.Ordinal);
        return history < 0 ? script[from..] : script[from..history];
    }
}
