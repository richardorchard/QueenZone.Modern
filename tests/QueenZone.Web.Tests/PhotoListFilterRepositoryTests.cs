using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class PhotoListFilterRepositoryTests
{
    [Fact]
    public async Task InMemory_GetCategoryPage_FiltersByDesktopPreset()
    {
        var repository = new InMemoryPhotoRepository(new SharedPhotoStore(SamplePhotoData.CreateSeedCategories()));
        var brian = (await repository.GetCategoriesAsync()).Single(c => c.Slug == "brian-may");

        var page = await repository.GetCategoryPageAsync(
            brian.CatId,
            1,
            24,
            new PhotoListFilter(PhotoSizePreset.Desktop));

        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(101, page.Items[0].PicId);
    }

    [Fact]
    public async Task InMemory_DetailNavigation_UsesFilteredNeighbors()
    {
        var repository = new InMemoryPhotoRepository(new SharedPhotoStore(SamplePhotoData.CreateSeedCategories()));
        var queen = (await repository.GetCategoriesAsync()).Single(c => c.Slug == "queen");
        var filter = new PhotoListFilter(PhotoSizePreset.Desktop);

        // Queen seed: 201=2560x1440 desktop; 204=1200x800 no; 203=800x600 no; 202=1080x1920 phone.
        var nav = await repository.GetDetailNavigationAsync(queen.CatId, 201, filter);
        Assert.NotNull(nav);
        Assert.Equal(0, nav.Index);
        Assert.Equal(1, nav.Count);
        Assert.Null(nav.PreviousPicId);
        Assert.Null(nav.NextPicId);

        Assert.Null(await repository.GetDetailNavigationAsync(queen.CatId, 202, filter));
    }

    [Fact]
    public async Task EfSqlite_GetCategoryPage_FiltersByLandscape()
    {
        await using var fixture = await SqlitePhotoFixture.CreateAsync();
        var repository = new EfPhotoRepository(fixture.DbContext, PhotoSqlQueries.CreateSqliteFixture());

        var page = await repository.GetCategoryPageAsync(3, 1, 10, new PhotoListFilter(PhotoSizePreset.Landscape));
        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, item => Assert.True(item.PictureWidth > item.PictureHeight));

        var desktop = await repository.GetCategoryPageAsync(3, 1, 10, new PhotoListFilter(PhotoSizePreset.Desktop));
        Assert.Equal(1, desktop.TotalCount);
        Assert.Equal(11, desktop.Items[0].PicId);

        var nav = await repository.GetDetailNavigationAsync(3, 11, new PhotoListFilter(PhotoSizePreset.Desktop));
        Assert.NotNull(nav);
        Assert.Equal(0, nav.Index);
        Assert.Equal(1, nav.Count);
    }
}

/// <summary>Minimal SQLite photo fixture for filter SQL (shares schema with EfPublicReadRepositoryTests).</summary>
internal sealed class SqlitePhotoFixture : IAsyncDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection connection;

    private SqlitePhotoFixture(Microsoft.Data.Sqlite.SqliteConnection connection, QueenZoneDbContext dbContext)
    {
        this.connection = connection;
        DbContext = dbContext;
    }

    public QueenZoneDbContext DbContext { get; }

    public static async Task<SqlitePhotoFixture> CreateAsync()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new QueenZoneDbContext(options);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE PhotoCategories (cat_id INTEGER NOT NULL, name TEXT NOT NULL);
            CREATE TABLE PhotoItems (
                NAME TEXT NOT NULL,
                DATE_TIME TEXT NOT NULL,
                URL TEXT NOT NULL,
                THUMB_URL TEXT NOT NULL,
                T_HEIGHT INTEGER NOT NULL,
                T_WIDTH INTEGER NOT NULL,
                PIC_WIDTH INTEGER NOT NULL,
                PIC_HEIGHT INTEGER NOT NULL,
                pic_id INTEGER NOT NULL,
                category_name TEXT,
                cat_id INTEGER NOT NULL
            );
            INSERT INTO PhotoCategories (cat_id, name) VALUES (3, 'Live 1986');
            INSERT INTO PhotoItems (NAME, DATE_TIME, URL, THUMB_URL, T_HEIGHT, T_WIDTH, PIC_WIDTH, PIC_HEIGHT, pic_id, category_name, cat_id)
            VALUES
                ('Newest', '1986-07-12 00:00:00', 'n.jpg', 'n-t.jpg', 100, 150, 1920, 1080, 11, 'Live 1986', 3),
                ('Middle', '1986-07-11 00:00:00', 'm.jpg', 'm-t.jpg', 100, 150, 800, 600, 10, 'Live 1986', 3),
                ('Oldest', '1986-07-10 00:00:00', 'o.jpg', 'o-t.jpg', 100, 150, 0, 0, 9, 'Live 1986', 3);
            """);
        return new SqlitePhotoFixture(connection, db);
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
