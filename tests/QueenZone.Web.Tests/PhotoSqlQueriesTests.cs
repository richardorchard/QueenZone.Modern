using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class PhotoSqlQueriesTests
{
    [Fact]
    public void CreateProduction_ReturnsSeekFriendlyCategoryAndNeighborSql()
    {
        var sql = PhotoSqlQueries.CreateProduction();

        Assert.Contains("GROUP BY c.cat_id, c.name", sql.CategoriesWithCountsSql, StringComparison.Ordinal);
        Assert.Contains("OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", sql.CategoryPageSql, StringComparison.Ordinal);
        Assert.Contains("UNION ALL", sql.PreviousPicIdSql, StringComparison.Ordinal);
        Assert.Contains("UNION ALL", sql.NextPicIdSql, StringComparison.Ordinal);
        Assert.Contains("Date_time > {1}", sql.IndexBeforeSql, StringComparison.Ordinal);
        Assert.Contains("Date_time = {1} AND PIC_ID > {2}", sql.IndexBeforeSql, StringComparison.Ordinal);
        Assert.Contains("AS PreviousPicId", sql.DetailNavigationSql, StringComparison.Ordinal);
        Assert.Contains("AS NextPicId", sql.DetailNavigationSql, StringComparison.Ordinal);
        Assert.Contains("AS TotalCount", sql.DetailNavigationSql, StringComparison.Ordinal);
        Assert.Contains("AS IndexBefore", sql.DetailNavigationSql, StringComparison.Ordinal);
        Assert.Contains("WHERE p.Cat_ID = {0} AND p.PIC_ID = {1} AND p.DISPLAY = 1", sql.DetailNavigationSql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY c.name, p.Date_time DESC, p.PIC_ID DESC", sql.SitemapSql, StringComparison.Ordinal);
        Assert.Contains("WHERE p.Cat_ID = {0} AND p.DISPLAY = 1", sql.CategoryAllSql, StringComparison.Ordinal);
        Assert.Contains("CAST(ISNULL(p.PIC_WIDTH, 0) AS int) AS PIC_WIDTH", sql.CategoryPageSql, StringComparison.Ordinal);
        Assert.Contains("CAST(ISNULL(p.PIC_HEIGHT, 0) AS int) AS PIC_HEIGHT", sql.CategoryPageSql, StringComparison.Ordinal);
        Assert.Contains("CAST(ISNULL(p.PIC_WIDTH, 0) AS int) AS PIC_WIDTH", sql.DetailNavigationSql, StringComparison.Ordinal);
        Assert.Contains("CAST(ISNULL(p.PIC_HEIGHT, 0) AS int) AS PIC_HEIGHT", sql.CategoryAllSql, StringComparison.Ordinal);
        Assert.Contains("{PHOTO_FILTER_P}", sql.CategoryPageSql, StringComparison.Ordinal);
        Assert.Contains("{PHOTO_FILTER_T}", sql.DetailNavigationSql, StringComparison.Ordinal);
        var filtered = sql.ApplyFilter(sql.CategoryCountSql, new PhotoListFilter(PhotoSizePreset.Desktop));
        Assert.DoesNotContain("{PHOTO_FILTER_P}", filtered, StringComparison.Ordinal);
        Assert.Contains("1920", filtered, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateSqliteFixture_ReturnsCompatibleShapes()
    {
        var sql = PhotoSqlQueries.CreateSqliteFixture();

        Assert.Contains("LIMIT {1} OFFSET {0}", sql.CategoryPageSql, StringComparison.Ordinal);
        Assert.Contains("UNION ALL", sql.PreviousPicIdSql, StringComparison.Ordinal);
        Assert.Contains("FROM PhotoItems", sql.CategoryCountSql, StringComparison.Ordinal);
        Assert.Contains("AS PreviousPicId", sql.DetailNavigationSql, StringComparison.Ordinal);
        Assert.Contains("AS NextPicId", sql.DetailNavigationSql, StringComparison.Ordinal);
        Assert.Contains("PIC_WIDTH", sql.CategoryPageSql, StringComparison.Ordinal);
        Assert.Contains("PIC_HEIGHT", sql.DetailNavigationSql, StringComparison.Ordinal);
    }
}
