using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sixpoints.PageOptimizer
{
    /// <summary>
    /// Represents a link in a breadcrumb navigation trail
    /// </summary>
    public class BreadcrumbLink
    {
        /// <summary>
        /// Gets or sets the display text for the breadcrumb
        /// </summary>
        public string? Title { get; set; }
        /// <summary>
        /// Gets or sets the URL for the breadcrumb. If null, the breadcrumb will not be clickable
        /// </summary>
        public string? Link { get; set; }

        /// <summary>
        /// Initialize a new breadcrumb link
        /// </summary>
        public BreadcrumbLink() { }

        /// <summary>
        /// Creates a new breadcrumb link with the specified title and link
        /// </summary>
        public BreadcrumbLink(string title, string? link = null)
        {
            Title = title;
            Link = link;
        }
    }
}
