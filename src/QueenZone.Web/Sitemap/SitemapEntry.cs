namespace QueenZone.Web.Sitemap;

public sealed record SitemapEntry(string Path, DateTime? LastModified = null);

public sealed record SitemapIndexEntry(string Path, DateTime? LastModified = null);