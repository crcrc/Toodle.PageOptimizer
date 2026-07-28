using System;

namespace Admirably.PageOptimizer.Rss.Models
{
    /// <summary>
    /// Represents a single item in an RSS feed.
    /// Link should be an absolute URL. Item limit per feed is the caller's responsibility.
    /// </summary>
    public class RssItem
    {
        /// <summary>The title of the item.</summary>
        public string Title { get; set; }

        /// <summary>The absolute URL of the item.</summary>
        public string Link { get; set; }

        /// <summary>A summary or full content of the item. Optional.</summary>
        public string? Description { get; set; }

        /// <summary>The publication date of the item. Optional.</summary>
        public DateTime? PublishedDate { get; set; }

        /// <summary>
        /// A unique identifier for the item. Defaults to Link if not set.
        /// </summary>
        public string? Guid { get; set; }

        /// <summary>The author of the item. Optional.</summary>
        public string? Author { get; set; }

        /// <summary>The category of the item. Optional.</summary>
        public string? Category { get; set; }
    }
}
