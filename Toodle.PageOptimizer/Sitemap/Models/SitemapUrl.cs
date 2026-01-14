using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toodle.PageOptimizer.Sitemap.Models
{
    public class SitemapUrl
    {
        /// <summary>
        /// The absolute URL of the page.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// The date of last modification of the file.
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// How frequently the page is likely to change.
        /// </summary>
        public ChangeFrequency? ChangeFrequency { get; set; }

        /// <summary>
        /// The priority of this URL relative to other URLs on your site.
        /// </summary>
        public decimal? Priority { get; set; }
    }

    public enum ChangeFrequency
    {
        Always,
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Yearly,
        Never
    }
}
