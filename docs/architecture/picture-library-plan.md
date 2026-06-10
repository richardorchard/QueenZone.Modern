# Picture Library Plan

## Goal

Bring the QueenZone picture library forward as a major public archive feature.

The legacy site had extensive picture records, and the image files may exist in backup or existing Blob Storage. The migration should preserve metadata, URLs where possible, and image quality while improving performance.

## Legacy Sources

Database:

- `PIC_FILES_T`
- `PIC_CAT_T`
- `Q_RANDOM_PIC_T`
- `Q_PIC_TAG_T`
- `Q_PICTURE_TAG_T`
- `Q_USERS_PICTURES_T` for user-submitted/member pictures, if ever included.

Important columns in `PIC_FILES_T`:

- `PIC_ID`
- `Name`
- `Cat_ID`
- `Date_time`
- `Url`
- `Thumb_URL`
- `t_height`
- `t_width`
- `user_id`
- `DISPLAY`
- `PIC_HEIGHT`
- `PIC_WIDTH`
- `KEYWORDS`
- `PICTURE_YEAR`

## Migration Strategy

### Stage 1: Inventory

- Count `PIC_FILES_T` rows.
- Count `DISPLAY = 1` rows.
- Group by category.
- Extract all `Url` and `Thumb_URL` paths.
- Compare DB paths against backup/blob inventory.
- Report missing originals and missing thumbnails.

### Stage 2: Canonical Asset Store

Use Azure Blob Storage as canonical storage for public images.

Suggested containers:

- `pictures-original`
- `pictures-web`
- `pictures-thumb`

Suggested path shape:

```text
pictures/{category-slug}/{pic-id}/{filename}
pictures/{category-slug}/{pic-id}/thumb.webp
pictures/{category-slug}/{pic-id}/medium.webp
```

### Stage 3: Responsive Variants

Generate modern variants:

- Thumbnail.
- Medium web image.
- Large web image.
- Original retained but not always linked directly.

Prefer modern formats where practical, while keeping originals available.

### Stage 4: Modern Read Model

Possible modern tables:

- `PictureCategory`
- `PictureAsset`
- `PictureVariant`
- `LegacyPictureSource`

Keep:

- Legacy `PIC_ID`.
- Original legacy URL.
- Blob URL.
- Dimensions.
- Caption/name.
- Category.
- Keywords.
- Year.

## SEO Requirements

- Image detail pages with stable canonical URLs.
- Category pages with crawlable thumbnails.
- Image dimensions in markup.
- Descriptive alt text.
- Lazy loading below the fold.
- Image sitemap for public images.
- Open Graph image on detail pages.

## Open Questions

- Are all images legally safe to republish?
- Are user-submitted/member images part of the initial picture library?
- Should originals be downloadable?
- Should EXIF metadata be stripped from public variants?
- Are current Blob URLs stable enough to preserve?

