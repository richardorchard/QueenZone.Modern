/*
QueenZone legacy performance indexes.

Purpose:
  Reviewed CREATE INDEX candidates from the read-only database health pass run
  on MAIN2_DB on 2026-06-28.

Scope:
  - Supports existing read-only public archive paths.
  - Does not change data.
  - Does not grant permissions.
  - Should be reviewed and applied manually in a maintenance window.

Measured signals:
  - PIC_FILES_T missing-index score: 3029.40 for Cat_ID + DISPLAY.
  - Q_FORUM_TOPIC_T missing-index score: 1083.73 for Q_FORUM_ID + TOPIC_STARTER.
  - Q_FORUM_TOPIC_T missing-index score: 1064.02 for Q_FORUM_ID + Q_FORUM_TOPIC_PARENT_ID.

Important:
  Do not run this blindly against production. Capture before/after timings for:
    - Q_PIC_CAT_PAGE4_SP
    - Q_FORUM_VIEW_PAGE_SP
    - Q_FORUM_TOPIC_NEW_SP

  These indexes add write overhead to the legacy tables. That is acceptable only
  if the target database is primarily serving the read-only archive path.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

PRINT 'Checking candidate legacy performance indexes...';

/*
Candidate 1: Picture category pages.

Supports category grids and neighbor navigation:
  WHERE PIC_FILES_T.Cat_ID = @CAT_ID
    AND PIC_FILES_T.DISPLAY = 1
  ORDER BY PIC_FILES_T.Date_time DESC, PIC_FILES_T.PIC_ID DESC

The leading equality keys match the filter. Date_time DESC + PIC_ID DESC match
the public sort so OFFSET/FETCH and TOP (1) neighbor seeks avoid TopN sorts.
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

/*
Candidate 2: Forum topic list pages.

Supports Q_FORUM_VIEW_PAGE_SP and Q_FORUM_TOPIC_NO_PARENT_V style reads:
  WHERE Q_FORUM_ID = @Q_FORUM_ID
    AND TOPIC_STARTER = 1
  ORDER BY STICKY DESC, TOPIC_LAST_POST DESC

The legacy table already has broader indexes, but the measured missing-index
request shows the existing key order is not selective enough for this path.
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
Candidate 3: Forum parent/thread grouping.

Supports forum archive reads that group or filter topics by forum plus parent.
The missing-index DMV requested Q_FORUM_ID + Q_FORUM_TOPIC_PARENT_ID with a
very high estimated impact. Keep includes narrow because Q_FORUM_TOPIC_T is the
largest legacy table in this database.
*/
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Q_FORUM_TOPIC_T')
      AND name = N'IX_Q_FORUM_TOPIC_T_Forum_Parent'
)
BEGIN
    PRINT 'Creating IX_Q_FORUM_TOPIC_T_Forum_Parent...';

    CREATE NONCLUSTERED INDEX IX_Q_FORUM_TOPIC_T_Forum_Parent
    ON dbo.Q_FORUM_TOPIC_T
    (
        Q_FORUM_ID ASC,
        Q_FORUM_TOPIC_PARENT_ID ASC,
        Q_FORUM_TOPIC_ID ASC
    )
    INCLUDE
    (
        TOPIC_STARTER,
        TOPIC_SUBJECT,
        TOPIC_LAST_POST
    )
    WITH (SORT_IN_TEMPDB = ON);
END
ELSE
BEGIN
    PRINT 'IX_Q_FORUM_TOPIC_T_Forum_Parent already exists; skipping.';
END;

PRINT 'Candidate legacy performance indexes complete.';

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
    N'IX_PIC_FILES_T_Cat_Display_Date',
    N'IX_Q_FORUM_TOPIC_T_Forum_Starter_LastPost',
    N'IX_Q_FORUM_TOPIC_T_Forum_Parent'
)
ORDER BY object_name, i.name;

Rollback, if a reviewed deployment needs to undo these indexes:

DROP INDEX IF EXISTS IX_PIC_FILES_T_Cat_Display_Date ON dbo.PIC_FILES_T;
DROP INDEX IF EXISTS IX_Q_FORUM_TOPIC_T_Forum_Starter_LastPost ON dbo.Q_FORUM_TOPIC_T;
DROP INDEX IF EXISTS IX_Q_FORUM_TOPIC_T_Forum_Parent ON dbo.Q_FORUM_TOPIC_T;
*/
