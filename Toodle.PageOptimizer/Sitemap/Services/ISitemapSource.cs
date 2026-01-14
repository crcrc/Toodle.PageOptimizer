using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toodle.PageOptimizer.Sitemap.Models;

namespace Toodle.PageOptimizer.Sitemap.Services
{
    public interface ISitemapSource
    {
        /// <summary>
        /// Asynchronously retrieves a collection of SitemapUrl objects.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation, containing an
        /// enumerable collection of SitemapUrl objects.
        /// </returns>
        Task<IEnumerable<SitemapUrl>> GetUrlsAsync();
    }
}
