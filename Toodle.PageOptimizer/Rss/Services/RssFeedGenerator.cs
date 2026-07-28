using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Admirably.PageOptimizer.Rss.Models;

namespace Admirably.PageOptimizer.Rss.Services
{
    /// <summary>
    /// Generates RSS 2.0 feed XML from a collection of RssItem objects.
    /// This class is stateless and safe to use as a singleton.
    /// </summary>
    public class RssFeedGenerator
    {
        /// <summary>
        /// Serializes a collection of RssItem objects into a valid RSS 2.0 XML string.
        /// </summary>
        /// <param name="items">The items to include in the feed.</param>
        /// <param name="title">The channel title.</param>
        /// <param name="description">The channel description.</param>
        /// <param name="channelLink">The absolute URL of the site (used as the channel link).</param>
        public string GenerateFeed(IEnumerable<RssItem> items, string title, string description, string channelLink)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(false)
            };

            using var stringWriter = new Sitemap.Services.Utf8StringWriter();
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                xmlWriter.WriteStartDocument();

                xmlWriter.WriteStartElement("rss");
                xmlWriter.WriteAttributeString("version", "2.0");

                xmlWriter.WriteStartElement("channel");
                xmlWriter.WriteElementString("title", title);
                xmlWriter.WriteElementString("link", channelLink);
                xmlWriter.WriteElementString("description", description);

                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Link))
                        continue;

                    xmlWriter.WriteStartElement("item");
                    xmlWriter.WriteElementString("title", item.Title);
                    xmlWriter.WriteElementString("link", item.Link);

                    if (!string.IsNullOrWhiteSpace(item.Description))
                        xmlWriter.WriteElementString("description", item.Description);

                    if (item.PublishedDate.HasValue)
                        xmlWriter.WriteElementString("pubDate", item.PublishedDate.Value.ToUniversalTime()
                            .ToString("ddd, dd MMM yyyy HH:mm:ss GMT", CultureInfo.InvariantCulture));

                    xmlWriter.WriteElementString("guid", item.Guid ?? item.Link);

                    if (!string.IsNullOrWhiteSpace(item.Author))
                        xmlWriter.WriteElementString("author", item.Author);

                    if (!string.IsNullOrWhiteSpace(item.Category))
                        xmlWriter.WriteElementString("category", item.Category);

                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement(); // channel
                xmlWriter.WriteEndElement(); // rss
                xmlWriter.WriteEndDocument();
                xmlWriter.Flush();
            }

            return stringWriter.ToString();
        }
    }
}
