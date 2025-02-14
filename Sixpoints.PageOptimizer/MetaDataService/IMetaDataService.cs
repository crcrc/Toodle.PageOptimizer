using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sixpoints.PageOptimizer
{
    /// <summary>
    /// Interface for managing metadata and breadcrumbs for web pages
    /// </summary>
    public interface IMetaDataService
    {
        /// <summary>
        /// Sets the meta title for the page
        /// </summary>
        IMetaDataService SetMetaTitle(string title);
        /// <summary>
        /// Sets the locale for the page
        /// </summary>
        IMetaDataService SetLocale(string locale);
        /// <summary>
        /// Sets the meta description for the page
        /// </summary>
        /// <param name="description">The description to be used in meta tags</param>
        IMetaDataService SetMetaDescription(string description);
        /// <summary>
        /// Adds a URL to the list of images to be preloaded
        /// </summary>
        /// <param name="url">The URL of the image to preload</param>
        IMetaDataService AddPreloadImage(string url);
        /// <summary>
        /// Sets the noindex flag to prevent search engines from indexing the page
        /// </summary>
        IMetaDataService SetNoIndex();
        /// <summary>
        /// Sets the canonical URL for the page
        /// </summary>
        /// <param name="relativePath">The relative path to be combined with the base URL</param>
        IMetaDataService SetCanonicalUrl(string relativePath);
        /// <summary>
        /// Adds a breadcrumb link to the navigation trail
        /// </summary>
        /// <param name="link">The breadcrumb link object containing title and URL</param>
        IMetaDataService AddBreadCrumb(BreadcrumbLink link);
        /// <summary>
        /// Adds a breadcrumb to the navigation trail
        /// </summary>
        /// <param name="title">The title of the breadcrumb</param>
        /// <param name="link">Optional URL for the breadcrumb. If null, the breadcrumb will not be clickable</param>
        IMetaDataService AddBreadCrumb(string title, string? link = null);

        /// <summary>
        /// Gets the current meta title
        /// </summary>
        /// <returns>The meta title string</returns>
        string GetMetaDescription();
        /// <summary>
        /// Gets the current meta description
        /// </summary>
        /// <returns>The meta description string</returns>
        string GetMetaTitle();
        /// <summary>
        /// Gets the list of image URLs to be preloaded
        /// </summary>
        /// <returns>An immutable set of image URLs</returns>
        IReadOnlySet<string> GetPreloadImageUrls();
        /// <summary>
        /// Gets whether the page should be indexed by search engines
        /// </summary>
        /// <returns>True if the page should not be indexed, false otherwise</returns>
        bool IsNoIndex();
        /// <summary>
        /// Gets the site name
        /// </summary>
        /// <returns>The name of the website</returns>
        public string GetSiteName();
        /// <summary>
        /// Gets the current breadcrumb navigation trail
        /// </summary>
        /// <returns>An immutable list of breadcrumb links</returns>
        IReadOnlyList<BreadcrumbLink> GetBreadCrumbs();
        /// <summary>
        /// Gets the canonical URL for the current page
        /// </summary>
        /// <returns>The canonical URL, or null if not set</returns>
        public Uri? GetCanonicalUrl();
        /// <summary>
        /// Gets the base URL of the website
        /// </summary>
        /// <returns>The base URL</returns>
        public Uri GetBaseUrl();
        /// <summary>
        /// Gets the current locale
        /// </summary>
        /// <returns>The locale code (e.g., en-US)</returns>
        public string GetLocale();
    }
}
