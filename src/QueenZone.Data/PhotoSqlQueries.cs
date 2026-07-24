namespace QueenZone.Data;

/// <summary>
/// SQL shapes for <see cref="EfPhotoRepository"/>. Production uses SQL Server;
/// tests substitute SQLite-compatible selects against fixture tables.
/// Placeholders follow EF <c>SqlQueryRaw</c> conventions: <c>{0}</c>, <c>{1}</c>, …
/// </summary>
public sealed class PhotoSqlQueries
{
    public required string CategoriesWithCountsSql { get; init; }

    /// <summary>Parameters: offset, pageSize, catId.</summary>
    public required string CategoryPageSql { get; init; }

    /// <summary>Parameter: catId. Returns scalar count as <c>Value</c>.</summary>
    public required string CategoryCountSql { get; init; }

    /// <summary>Parameter: catId. Returns <c>name</c>.</summary>
    public required string CategoryNameSql { get; init; }

    /// <summary>Parameters: catId, picId.</summary>
    public required string PhotoByIdSql { get; init; }

    /// <summary>Parameters: catId, dateTime, picId — neighbor toward newer (list index - 1).</summary>
    public required string PreviousPicIdSql { get; init; }

    /// <summary>Parameters: catId, dateTime, picId — neighbor toward older (list index + 1).</summary>
    public required string NextPicIdSql { get; init; }

    /// <summary>Parameters: catId, dateTime, picId — count of photos before current in DESC order.</summary>
    public required string IndexBeforeSql { get; init; }

    /// <summary>Light sitemap rows: cat_id, category_name, pic_id, date_time.</summary>
    public required string SitemapSql { get; init; }

    /// <summary>Parameter: catId. Full visible collection for tools (not detail pages).</summary>
    public required string CategoryAllSql { get; init; }

    public static PhotoSqlQueries CreateProduction() =>
        new()
        {
            CategoriesWithCountsSql = """
                SELECT
                    c.cat_id,
                    c.name,
                    COUNT(p.PIC_ID) AS ImageCount,
                    (
                        SELECT TOP (1) p2.Thumb_URL
                        FROM dbo.PIC_FILES_T p2
                        WHERE p2.Cat_ID = c.cat_id AND p2.DISPLAY = 1
                        ORDER BY p2.Date_time DESC, p2.PIC_ID DESC
                    ) AS CoverThumbUrl
                FROM dbo.PIC_CAT_T c
                INNER JOIN dbo.PIC_FILES_T p ON p.Cat_ID = c.cat_id AND p.DISPLAY = 1
                GROUP BY c.cat_id, c.name
                HAVING COUNT(p.PIC_ID) > 0
                ORDER BY c.name
                """,
            CategoryPageSql = """
                SELECT
                    p.Name AS NAME,
                    p.Date_time AS DATE_TIME,
                    p.Url AS URL,
                    p.Thumb_URL AS THUMB_URL,
                    ISNULL(p.t_height, 0) AS T_HEIGHT,
                    ISNULL(p.t_width, 0) AS T_WIDTH,
                    p.PIC_ID AS pic_id,
                    c.name AS category_name
                FROM dbo.PIC_FILES_T p
                INNER JOIN dbo.PIC_CAT_T c ON c.cat_id = p.Cat_ID
                WHERE p.Cat_ID = {2} AND p.DISPLAY = 1
                ORDER BY p.Date_time DESC, p.PIC_ID DESC
                OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY
                """,
            CategoryCountSql = """
                SELECT COUNT(*) AS Value
                FROM dbo.PIC_FILES_T
                WHERE Cat_ID = {0} AND DISPLAY = 1
                """,
            CategoryNameSql = """
                SELECT name
                FROM dbo.PIC_CAT_T
                WHERE cat_id = {0}
                """,
            PhotoByIdSql = """
                SELECT
                    p.Name AS NAME,
                    p.Date_time AS DATE_TIME,
                    p.Url AS URL,
                    p.Thumb_URL AS THUMB_URL,
                    ISNULL(p.t_height, 0) AS T_HEIGHT,
                    ISNULL(p.t_width, 0) AS T_WIDTH,
                    p.PIC_ID AS pic_id,
                    c.name AS category_name
                FROM dbo.PIC_FILES_T p
                INNER JOIN dbo.PIC_CAT_T c ON c.cat_id = p.Cat_ID
                WHERE p.Cat_ID = {0} AND p.PIC_ID = {1} AND p.DISPLAY = 1
                """,
            PreviousPicIdSql = """
                SELECT TOP (1) PIC_ID AS Value
                FROM dbo.PIC_FILES_T
                WHERE Cat_ID = {0}
                  AND DISPLAY = 1
                  AND (Date_time > {1} OR (Date_time = {1} AND PIC_ID > {2}))
                ORDER BY Date_time ASC, PIC_ID ASC
                """,
            NextPicIdSql = """
                SELECT TOP (1) PIC_ID AS Value
                FROM dbo.PIC_FILES_T
                WHERE Cat_ID = {0}
                  AND DISPLAY = 1
                  AND (Date_time < {1} OR (Date_time = {1} AND PIC_ID < {2}))
                ORDER BY Date_time DESC, PIC_ID DESC
                """,
            IndexBeforeSql = """
                SELECT COUNT(*) AS Value
                FROM dbo.PIC_FILES_T
                WHERE Cat_ID = {0}
                  AND DISPLAY = 1
                  AND (Date_time > {1} OR (Date_time = {1} AND PIC_ID > {2}))
                """,
            SitemapSql = """
                SELECT
                    c.cat_id,
                    c.name AS category_name,
                    p.PIC_ID AS pic_id,
                    p.Date_time AS date_time
                FROM dbo.PIC_FILES_T p
                INNER JOIN dbo.PIC_CAT_T c ON c.cat_id = p.Cat_ID
                WHERE p.DISPLAY = 1
                ORDER BY c.name, p.Date_time DESC, p.PIC_ID DESC
                """,
            CategoryAllSql = """
                SELECT
                    p.Name AS NAME,
                    p.Date_time AS DATE_TIME,
                    p.Url AS URL,
                    p.Thumb_URL AS THUMB_URL,
                    ISNULL(p.t_height, 0) AS T_HEIGHT,
                    ISNULL(p.t_width, 0) AS T_WIDTH,
                    p.PIC_ID AS pic_id,
                    c.name AS category_name
                FROM dbo.PIC_FILES_T p
                INNER JOIN dbo.PIC_CAT_T c ON c.cat_id = p.Cat_ID
                WHERE p.Cat_ID = {0} AND p.DISPLAY = 1
                ORDER BY p.Date_time DESC, p.PIC_ID DESC
                """,
        };

    /// <summary>
    /// SQLite fixture shapes used by <c>EfPublicReadRepositoryTests</c>
    /// (<c>PhotoCategories</c> / <c>PhotoItems</c> tables).
    /// </summary>
    public static PhotoSqlQueries CreateSqliteFixture() =>
        new()
        {
            CategoriesWithCountsSql = """
                SELECT
                    c.cat_id,
                    c.name,
                    COUNT(p.pic_id) AS ImageCount,
                    (
                        SELECT p2.THUMB_URL
                        FROM PhotoItems p2
                        WHERE p2.cat_id = c.cat_id
                        ORDER BY p2.DATE_TIME DESC, p2.pic_id DESC
                        LIMIT 1
                    ) AS CoverThumbUrl
                FROM PhotoCategories c
                INNER JOIN PhotoItems p ON p.cat_id = c.cat_id
                GROUP BY c.cat_id, c.name
                HAVING COUNT(p.pic_id) > 0
                ORDER BY c.name
                """,
            CategoryPageSql = """
                SELECT NAME, DATE_TIME, URL, THUMB_URL, T_HEIGHT, T_WIDTH, pic_id, category_name
                FROM PhotoItems
                WHERE cat_id = {2}
                ORDER BY DATE_TIME DESC, pic_id DESC
                LIMIT {1} OFFSET {0}
                """,
            CategoryCountSql = """
                SELECT COUNT(*) AS Value FROM PhotoItems WHERE cat_id = {0}
                """,
            CategoryNameSql = """
                SELECT name FROM PhotoCategories WHERE cat_id = {0}
                """,
            PhotoByIdSql = """
                SELECT NAME, DATE_TIME, URL, THUMB_URL, T_HEIGHT, T_WIDTH, pic_id, category_name
                FROM PhotoItems
                WHERE cat_id = {0} AND pic_id = {1}
                """,
            PreviousPicIdSql = """
                SELECT pic_id AS Value
                FROM PhotoItems
                WHERE cat_id = {0}
                  AND (DATE_TIME > {1} OR (DATE_TIME = {1} AND pic_id > {2}))
                ORDER BY DATE_TIME ASC, pic_id ASC
                LIMIT 1
                """,
            NextPicIdSql = """
                SELECT pic_id AS Value
                FROM PhotoItems
                WHERE cat_id = {0}
                  AND (DATE_TIME < {1} OR (DATE_TIME = {1} AND pic_id < {2}))
                ORDER BY DATE_TIME DESC, pic_id DESC
                LIMIT 1
                """,
            IndexBeforeSql = """
                SELECT COUNT(*) AS Value
                FROM PhotoItems
                WHERE cat_id = {0}
                  AND (DATE_TIME > {1} OR (DATE_TIME = {1} AND pic_id > {2}))
                """,
            SitemapSql = """
                SELECT
                    c.cat_id,
                    c.name AS category_name,
                    p.pic_id,
                    p.DATE_TIME AS date_time
                FROM PhotoItems p
                INNER JOIN PhotoCategories c ON c.cat_id = p.cat_id
                ORDER BY c.name, p.DATE_TIME DESC, p.pic_id DESC
                """,
            CategoryAllSql = """
                SELECT NAME, DATE_TIME, URL, THUMB_URL, T_HEIGHT, T_WIDTH, pic_id, category_name
                FROM PhotoItems
                WHERE cat_id = {0}
                ORDER BY DATE_TIME DESC, pic_id DESC
                """,
        };
}
