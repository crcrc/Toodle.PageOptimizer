using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Toodle.PageOptimizer.Sitemap.Models;

namespace Toodle.PageOptimizer.Sitemap.Services
{
    /// <summary>
    /// Generates a sitemap XML string from a collection of SitemapUrl objects.
    /// This class is stateless and can be registered as a scoped or transient service.
    /// </summary>
    public class SitemapGenerator
    {
        /// <summary>
        /// Serializes a collection of SitemapUrl objects into a valid sitemap XML string.
        /// </summary>
        /// <param name="urls">An enumerable collection of SitemapUrl objects to include.</param>
        /// <returns>A string containing the complete sitemap.xml content.</returns>
        public string GenerateSitemap(IEnumerable<SitemapUrl> urls)
        {
            // Configure the XmlWriter to produce indented, human-readable XML
            // with UTF-8 encoding and without a Byte Order Mark (BOM).
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(false)
            };

            // Using a StringWriter as the destination for the XML stream.
            // It writes to an in-memory StringBuilder.
            using (var stringWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
                {
                    // Write the XML declaration: <?xml version="1.0" encoding="utf-8"?>
                    xmlWriter.WriteStartDocument();

                    // Write the root <urlset> element with its required namespace.
                    // This namespace is critical for search engine crawlers to validate the sitemap.
                    xmlWriter.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                    foreach (var url in urls)
                    {
                        // Skip any entries that have a null or empty location, as this is invalid.
                        if (string.IsNullOrWhiteSpace(url.Location))
                        {
                            continue;
                        }

                        // Start the <url> element for this entry.
                        xmlWriter.WriteStartElement("url");

                        // Write the mandatory <loc> element.
                        xmlWriter.WriteElementString("loc", url.Location);

                        // Write the optional <lastmod> element if a value is provided.
                        // It must be formatted in "YYYY-MM-DD" format.
                        if (url.LastModified.HasValue)
                        {
                            xmlWriter.WriteElementString("lastmod", url.LastModified.Value.ToString("yyyy-MM-dd"));
                        }

                        // Write the optional <changefreq> element if a value is provided.
                        // The value is the enum's name converted to lowercase (e.g., "daily").
                        if (url.ChangeFrequency.HasValue)
                        {
                            xmlWriter.WriteElementString("changefreq", url.ChangeFrequency.ToString()!.ToLowerInvariant());
                        }

                        // Write the optional <priority> element if a value is provided.
                        if (url.Priority.HasValue)
                        {
                            // The sitemap protocol requires the priority to be between 0.0 and 1.0.
                            // We clamp the value to ensure it's always valid, even with bad input.
                            var priority = Math.Max(0.0m, Math.Min(1.0m, url.Priority.Value));

                            // Format to one decimal place (e.g., "0.9"). CultureInfo.InvariantCulture
                            // ensures a period is used as the decimal separator regardless of server locale.
                            xmlWriter.WriteElementString("priority", priority.ToString("F1", CultureInfo.InvariantCulture));
                        }

                        // Close the </url> element.
                        xmlWriter.WriteEndElement();
                    }

                    // Close the root </urlset> element.
                    xmlWriter.WriteEndElement();

                    // Finalize the XML document.
                    xmlWriter.WriteEndDocument();
                    xmlWriter.Flush();
                }

                // Return the complete XML string from the StringWriter.
                return stringWriter.ToString();
            }
        }
    }
}
