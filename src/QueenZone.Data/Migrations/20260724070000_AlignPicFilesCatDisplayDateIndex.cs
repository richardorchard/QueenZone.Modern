using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Aligns <c>IX_PIC_FILES_T_Cat_Display_Date</c> with public photography sort order
/// (<c>Date_time DESC, PIC_ID DESC</c>) so category pages and neighbor seeks avoid TopN sorts.
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260724070000_AlignPicFilesCatDisplayDateIndex")]
public partial class AlignPicFilesCatDisplayDateIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.PIC_FILES_T', N'U') IS NULL
            BEGIN
                RETURN;
            END;

            DECLARE @needsRebuild bit = 0;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'dbo.PIC_FILES_T', N'U')
                  AND name = N'IX_PIC_FILES_T_Cat_Display_Date')
            BEGIN
                SET @needsRebuild = 1;
            END
            ELSE IF EXISTS (
                SELECT 1
                FROM sys.indexes AS i
                INNER JOIN sys.index_columns AS ic
                    ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                INNER JOIN sys.columns AS c
                    ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE i.object_id = OBJECT_ID(N'dbo.PIC_FILES_T', N'U')
                  AND i.name = N'IX_PIC_FILES_T_Cat_Display_Date'
                  AND c.name = N'PIC_ID'
                  AND ic.is_included_column = 0
                  AND ic.is_descending_key = 0)
            BEGIN
                SET @needsRebuild = 1;
            END;

            IF @needsRebuild = 1
            BEGIN
                DROP INDEX IF EXISTS IX_PIC_FILES_T_Cat_Display_Date ON dbo.PIC_FILES_T;

                CREATE NONCLUSTERED INDEX IX_PIC_FILES_T_Cat_Display_Date
                ON dbo.PIC_FILES_T
                (
                    Cat_ID ASC,
                    DISPLAY ASC,
                    Date_time DESC,
                    PIC_ID DESC
                )
                INCLUDE
                (
                    Name,
                    Url,
                    Thumb_URL,
                    t_height,
                    t_width,
                    user_id
                )
                WITH (SORT_IN_TEMPDB = ON);
            END
            """, suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.PIC_FILES_T', N'U') IS NULL
            BEGIN
                RETURN;
            END;

            DROP INDEX IF EXISTS IX_PIC_FILES_T_Cat_Display_Date ON dbo.PIC_FILES_T;

            CREATE NONCLUSTERED INDEX IX_PIC_FILES_T_Cat_Display_Date
            ON dbo.PIC_FILES_T
            (
                Cat_ID ASC,
                DISPLAY ASC,
                Date_time DESC,
                PIC_ID ASC
            )
            INCLUDE
            (
                Name,
                Url,
                Thumb_URL,
                t_height,
                t_width,
                user_id
            )
            WITH (SORT_IN_TEMPDB = ON);
            """, suppressTransaction: true);
    }
}
