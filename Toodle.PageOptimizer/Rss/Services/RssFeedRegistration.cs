using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Admirably.PageOptimizer.Rss.Models;

namespace Admirably.PageOptimizer.Rss.Services
{
    /// <summary>
    /// Holds the configuration and source function for a single RSS feed.
    /// </summary>
    public class RssFeedRegistration
    {
        /// <summary>The path at which this feed is served (e.g. "/feed.xml").</summary>
        public string Path { get; init; }

        /// <summary>The RSS channel title.</summary>
        public string Title { get; init; }

        /// <summary>The RSS channel description.</summary>
        public string Description { get; init; }

        /// <summary>How long the generated feed XML is cached before being refreshed.</summary>
        public TimeSpan CacheDuration { get; init; }

        /// <summary>The function that provides the feed items.</summary>
        public Func<IServiceProvider, Task<IEnumerable<RssItem>>> Source { get; init; }
    }
}
