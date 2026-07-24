/*
QueenZone live Azure SQL performance indexes.

Purpose:
  Reviewed CREATE INDEX candidates from the read-only live database health pass
  run against queenzone-db on 2026-06-28.

Scope:
  - Supports existing public archive read paths.
  - Does not change data.
  - Does not grant permissions.
  - Should be applied manually in a quiet maintenance window only after review.

Live measured signals:
  - Q_FORUM_TOPIC_T missing-index score: 822237.81 for Q_FORUM_ID + TOPIC_STARTER.
  - NEWS_T missing-index score: 29371.57 for DISPLAY including DATE.
  - PIC_FILES_T missing-index score: 27731.03 for Cat_ID + DISPLAY.

Live timing notes before these indexes:
  - Q_FORUM_VIEW_PAGE_SP page 1: about 2.6 seconds elapsed.
  - Q_FORUM_TOPIC_NEW_SP page 1: about 187 ms elapsed.
  - Q_PIC_CAT_PAGE4_SP largest category: about 662 ms elapsed.
  - News archive page query: about 6 ms elapsed, but still scans NEWS_T.

Important:
  This script differs from docs/sql/002-legacy-performance-indexes.sql because
  the live missing-index evidence did not show the same forum-parent request.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

PRINT 'Checking live legacy performance indexes...';

/*
Candidate 1: Forum topic list pages.

Supports Q_FORUM_VIEW_PAGE_SP and Q_FORUM_TOPIC_NO_PARENT_V style reads:
  WHERE Q_FORUM_ID = @Q_FORUM_ID
    AND TOPIC_STARTER = 1
  ORDER BY STICKY DESC, TOPIC_LAST_POST DESC
*/
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Q_FORUM_TOPIC_T')
      AND name = N'IX_Q_FORUM_TOPIC_T_Forum_Starter_LastPost'
)
BEGIN
    PRINT 'Creating IX_Q_FORUM_TOPIC_T_Forum_Starter_LastPost...';

    CREATE NONCLUSTERED INDEX IX_Q_FORUM_TOPIC_T_Forum_Starter_LastPost
    ON dbo.Q_FORUM_TOPIC_T
    (
        Q_FORUM_ID ASC,
        TOPIC_STARTER ASC,
        STICKY DESC,
        TOPIC_LAST_POST DESC,
        Q_FORUM_TOPIC_ID ASC
    )
    INCLUDE
    (
        TOPIC_SUBJECT,
        TOPIC_REPLIES,
        USER_ID,
        LAST_USER_ID,
        DISCOGRAPHY
    )
    WITH (SORT_IN_TEMPDB = ON);
END
ELSE
BEGIN
    PRINT 'IX_Q_FORUM_TOPIC_T_Forum_Starter_LastPost already exists; skipping.';
END;

/*
Candidate 2: News archive/latest news reads.

Supports LegacyNewsRepository archive/latest/sitemap paths:
  WHERE DISPLAY = 1
  ORDER BY [DATE] DESC, NEWS_ID DESC

The live database currently has a NEWS_T nonclustered index on USER_ID, which
does not support public archive reads.
*/
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.NEWS_T')
      AND name = N'IX_NEWS_T_Display_Date'
)
BEGIN
    PRINT 'Creating IX_NEWS_T_Display_Date...';

    CREATE NONCLUSTERED INDEX IX_NEWS_T_Display_Date
    ON dbo.NEWS_T
    (
        DISPLAY ASC,
        [DATE] DESC,
        NEWS_ID DESC
    )
    INCLUDE
    (
        TITLE,
        EXCERPT,
        SOURCE_URL,
        SLUG
    )
    WITH (SORT_IN_TEMPDB = ON);
END
ELSE
BEGIN
    PRINT 'IX_NEWS_T_Display_Date already exists; skipping.';
END;

/*
Candidate 3: Picture category pages.

Supports category grids and neighbor navigation:
  WHERE PIC_FILES_T.Cat_ID = @CAT_ID
    AND PIC_FILES_T.DISPLAY = 1
  ORDER BY PIC_FILES_T.Date_time DESC, PIC_FILES_T.PIC_ID DESC

Date_time DESC + PIC_ID DESC match the public sort so OFFSET/FETCH and TOP (1)
neighbor seeks avoid TopN sorts.
*/
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PIC_FILES_T')
      AND name = N'IX_PIC_FILES_T_Cat_Display_Date'
)
BEGIN
    PRINT 'Creating IX_PIC_FILES_T_Cat_Display_Date...';

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
ELSE
BEGIN
    PRINT 'IX_PIC_FILES_T_Cat_Display_Date already exists; skipping.';
END;

PRINT 'Live legacy performance index candidates complete.';

/*
Post-apply review queries:

SELECT
    OBJECT_SCHEMA_NAME(i.object_id) + '.' + OBJECT_NAME(i.object_id) AS object_name,
    i.name,
    i.type_desc,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats s
    ON s.database_id = DB_ID()
   AND s.object_id = i.object_id
   AND s.index_id = i.index_id
WHERE i.name IN (
    N'IX_Q_FORUM_TOPIC_T_Forum_Starter_LastPost',
    N'IX_NEWS_T_Display_Date',
    N'IX_PIC_FILES_T_Cat_Display_Date'
)
ORDER BY object_name, i.name;

Rollback, if a reviewed deployment needs to undo these indexes:

DROP INDEX IF EXISTS IX_Q_FORUM_TOPIC_T_Forum_Starter_LastPost ON dbo.Q_FORUM_TOPIC_T;
DROP INDEX IF EXISTS IX_NEWS_T_Display_Date ON dbo.NEWS_T;
DROP INDEX IF EXISTS IX_PIC_FILES_T_Cat_Display_Date ON dbo.PIC_FILES_T;
*/
