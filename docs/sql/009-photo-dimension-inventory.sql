-- Photo original-dimension inventory for public gallery rows (DISPLAY = 1).
-- Read-only. Supports GitHub issue #435 / epic #434.
--
-- Uses PIC_WIDTH / PIC_HEIGHT (original size), NOT t_width / t_height (thumbs).
-- Zero means unknown/unset for legacy rows.
--
-- Run against the legacy QueenZone database (ConnectionStrings__QueenZoneLegacy).
-- Prefer the tools command for formatted output:
--   dotnet run --project src/QueenZone.Tools -- photo-dim-inventory --connection-string "<cs>"

SET NOCOUNT ON;

DECLARE @Total int =
(
    SELECT COUNT(*)
    FROM dbo.PIC_FILES_T
    WHERE DISPLAY = 1
);

DECLARE @Usable int =
(
    SELECT COUNT(*)
    FROM dbo.PIC_FILES_T
    WHERE DISPLAY = 1
      AND PIC_WIDTH > 0
      AND PIC_HEIGHT > 0
);

SELECT
    @Total AS TotalPublic,
    @Usable AS UsableDimensions,
    @Total - @Usable AS MissingOrZeroDimensions,
    CASE WHEN @Total = 0 THEN 0.0 ELSE 100.0 * @Usable / @Total END AS UsablePercent;

-- Draft wallpaper candidates (epic #434 starting thresholds)
SELECT
    SUM(CASE WHEN PIC_WIDTH >= 1920 AND PIC_WIDTH >= PIC_HEIGHT AND PIC_WIDTH > 0 AND PIC_HEIGHT > 0 THEN 1 ELSE 0 END)
        AS DesktopWallpaperCandidates,
    SUM(CASE WHEN PIC_HEIGHT >= 1920 AND PIC_HEIGHT > PIC_WIDTH AND PIC_WIDTH > 0 AND PIC_HEIGHT > 0 THEN 1 ELSE 0 END)
        AS PhoneWallpaperCandidates,
    SUM(CASE WHEN (CASE WHEN PIC_WIDTH > PIC_HEIGHT THEN PIC_WIDTH ELSE PIC_HEIGHT END) >= 1920
                  AND PIC_WIDTH > 0 AND PIC_HEIGHT > 0 THEN 1 ELSE 0 END)
        AS LargeCandidates
FROM dbo.PIC_FILES_T
WHERE DISPLAY = 1;

-- Orientation among usable rows
SELECT
    SUM(CASE WHEN PIC_WIDTH > PIC_HEIGHT THEN 1 ELSE 0 END) AS LandscapeUsable,
    SUM(CASE WHEN PIC_HEIGHT > PIC_WIDTH THEN 1 ELSE 0 END) AS PortraitUsable,
    SUM(CASE WHEN PIC_WIDTH = PIC_HEIGHT THEN 1 ELSE 0 END) AS SquareUsable
FROM dbo.PIC_FILES_T
WHERE DISPLAY = 1
  AND PIC_WIDTH > 0
  AND PIC_HEIGHT > 0;

-- Longest-side buckets (includes zero-dim rows in the 0 bucket)
SELECT
    BucketLabel,
    COUNT(*) AS PhotoCount
FROM
(
    SELECT
        CASE
            WHEN PIC_WIDTH <= 0 OR PIC_HEIGHT <= 0 THEN '0 (unknown/zero longest)'
            WHEN CASE WHEN PIC_WIDTH > PIC_HEIGHT THEN PIC_WIDTH ELSE PIC_HEIGHT END BETWEEN 1 AND 799 THEN '1–799'
            WHEN CASE WHEN PIC_WIDTH > PIC_HEIGHT THEN PIC_WIDTH ELSE PIC_HEIGHT END BETWEEN 800 AND 1279 THEN '800–1279'
            WHEN CASE WHEN PIC_WIDTH > PIC_HEIGHT THEN PIC_WIDTH ELSE PIC_HEIGHT END BETWEEN 1280 AND 1919 THEN '1280–1919'
            WHEN CASE WHEN PIC_WIDTH > PIC_HEIGHT THEN PIC_WIDTH ELSE PIC_HEIGHT END BETWEEN 1920 AND 2559 THEN '1920–2559'
            ELSE '2560+'
        END AS BucketLabel
    FROM dbo.PIC_FILES_T
    WHERE DISPLAY = 1
) buckets
GROUP BY BucketLabel
ORDER BY
    CASE BucketLabel
        WHEN '0 (unknown/zero longest)' THEN 0
        WHEN '1–799' THEN 1
        WHEN '800–1279' THEN 2
        WHEN '1280–1919' THEN 3
        WHEN '1920–2559' THEN 4
        ELSE 5
    END;

-- Optional: top categories by public count with usable-dim rate
SELECT TOP (20)
    c.cat_id,
    c.name,
    COUNT(*) AS PublicCount,
    SUM(CASE WHEN p.PIC_WIDTH > 0 AND p.PIC_HEIGHT > 0 THEN 1 ELSE 0 END) AS UsableCount
FROM dbo.PIC_FILES_T p
INNER JOIN dbo.PIC_CAT_T c ON c.cat_id = p.Cat_ID
WHERE p.DISPLAY = 1
GROUP BY c.cat_id, c.name
ORDER BY PublicCount DESC;
