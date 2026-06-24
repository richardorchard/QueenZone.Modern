using System.Globalization;
using System.Text;
using System.Xml;

namespace QueenZone.Web.Sitemap;

public static class SitemapXmlWriter
{
    private const string SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public static string WriteUrlSet(IEnumerable<SitemapEntry> entries, string publicBaseUrl)
    {
        var settings = CreateWriterSettings();
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", SitemapNamespace);
            foreach (var entry in entries)
            {
                writer.WriteStartElement("url", SitemapNamespace);
                writer.WriteElementString("loc", SitemapNamespace, SiteUrl.ToAbsolute(publicBaseUrl, entry.Path));
                if (entry.LastModified is DateTime lastModified)
                {
                    writer.WriteElementString("lastmod", SitemapNamespace, FormatLastModified(lastModified));
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return builder.ToString();
    }

    public static string WriteSitemapIndex(IEnumerable<SitemapIndexEntry> sitemaps, string publicBaseUrl)
    {
        var settings = CreateWriterSettings();
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("sitemapindex", SitemapNamespace);
            foreach (var sitemap in sitemaps)
            {
                writer.WriteStartElement("sitemap", SitemapNamespace);
                writer.WriteElementString("loc", SitemapNamespace, SiteUrl.ToAbsolute(publicBaseUrl, sitemap.Path));
                if (sitemap.LastModified is DateTime lastModified)
                {
                    writer.WriteElementString("lastmod", SitemapNamespace, FormatLastModified(lastModified));
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return builder.ToString();
    }

    private static XmlWriterSettings CreateWriterSettings() =>
        new()
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            OmitXmlDeclaration = false,
            NewLineChars = "\n"
        };

    internal static string FormatLastModified(DateTime value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}