using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toodle.PageOptimizer.Sitemap.Models
{
    public class SitemapOptions
    {
        /// <summary>
        /// The duration for which the generated sitemap XML is cached.
        /// Default is 6 hours.
        /// </summary>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(6);
    }
}
