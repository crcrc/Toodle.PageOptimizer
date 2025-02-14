using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sixpoints.PageOptimizer
{
    /// <summary>
    /// Service for managing metadata and breadcrumbs for web pages
    /// </summary>
    public class MetaDataService : IMetaDataService
    {
        private readonly string _siteName;
        private readonly Uri _baseUrl;
        private readonly HashSet<string> _preloadImageUrls = new();
        private string _metaTitle = string.Empty;
        private Uri? _canonicalUrl;
        private string _metaDescription = string.Empty;
        private bool _noIndex = false;
        private string _locale;
        private readonly List<BreadcrumbLink> _breadcrumbs = new();

        /// <summary>
        /// Initializes a new instance of the MetaDataService
        /// </summary>
        /// <param name="siteName">The name of the website</param>
        /// <param name="baseUrl">The base URL of the website (e.g., https://example.com)</param>
        /// <param name="locale">The default locale for the website (e.g., en-US)</param>
        /// <param name="homeText">The text that will appear for the first BreadcrumbLink to the site root (e.g. Home)</param>
        /// <exception cref="ArgumentException">Thrown when siteName or baseUrl is null or empty, or if baseUrl is invalid</exception>
        public MetaDataService(string siteName, string baseUrl, string locale, string homeText = "Home")
        {
            if (string.IsNullOrEmpty(siteName))
                throw new ArgumentException("Site name cannot be empty", nameof(siteName));
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentException("Base URL cannot be empty", nameof(baseUrl));
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
                throw new ArgumentException("Invalid base URL", nameof(baseUrl));

            _siteName = siteName;
            _baseUrl = new Uri(baseUrl);
            _breadcrumbs.Add(new BreadcrumbLink { Title = "Home", Link = "/" });
            _locale = locale;
        }

        /// <summary>
        /// Sets the meta title for the page
        /// </summary>
        /// <param name="title">The title to be used in the meta tags</param>
        /// <returns>The current instance for method chaining</returns>
        public IMetaDataService SetMetaTitle(string title)
        {
            _metaTitle = title;
            return this;
        }

        /// <summary>
        /// Sets the locale for the page
        /// </summary>
        /// <param name="locale">The locale code (e.g., en-US)</param>
        /// <returns>The current instance for method chaining</returns>
        public IMetaDataService SetLocale(string locale)
        {
            _locale = locale;
            return this;
        }

        /// <summary>
        /// Sets the meta description for the page
        /// </summary>
        /// <param name="description">The description to be used in meta tags</param>
        /// <returns>The current instance for method chaining</returns>
        public IMetaDataService SetMetaDescription(string description)
        {
            _metaDescription = description;
            return this;
        }

        /// <summary>
        /// Adds a URL to the list of images to be preloaded
        /// </summary>
        /// <param name="url">The URL of the image to preload</param>
        /// <returns>The current instance for method chaining</returns>
        public IMetaDataService AddPreloadImage(string url)
        {
            _preloadImageUrls.Add(url);
            return this;
        }

        /// <summary>
        /// Sets the noindex flag to prevent search engines from indexing the page
        /// </summary>
        /// <returns>The current instance for method chaining</returns>
        public IMetaDataService SetNoIndex()
        {
            _noIndex = true;
            return this;
        }

        /// <summary>
        /// Sets the canonical URL for the page
        /// </summary>
        /// <param name="relativePath">The relative path to be combined with the base URL</param>
        /// <returns>The current instance for method chaining</returns>
        public IMetaDataService SetCanonicalUrl(string relativePath)
        {
            _canonicalUrl = new Uri(_baseUrl, relativePath);
            return this;
        }

        /// <summary>
        /// Adds a breadcrumb link to the navigation trail
        /// </summary>
        /// <param name="link">The breadcrumb link object containing title and URL</param>
        /// <returns>The current instance for method chaining</returns>
        public IMetaDataService AddBreadCrumb(BreadcrumbLink link)
        {
            _breadcrumbs.Add(link);
            return this;
        }

        /// <summary>
        /// Adds a breadcrumb to the navigation trail
        /// </summary>
        /// <param name="title">The title of the breadcrumb</param>
        /// <param name="link">Optional URL for the breadcrumb. If null, the breadcrumb will not be clickable</param>
        /// <returns>The current instance for method chaining</returns>
        public IMetaDataService AddBreadCrumb(string title, string? link = null)
        {
            _breadcrumbs.Add(new BreadcrumbLink { Title = title, Link = link });
            return this;
        }

        /// <summary>
        /// Clears all breadcrumbs and resets to just the home page
        /// </summary>
        /// <returns>The current instance for method chaining</returns>
        public IMetaDataService ClearBreadcrumbs()
        {
            _breadcrumbs.Clear();
            _breadcrumbs.Add(new BreadcrumbLink { Title = "Home", Link = "/" });
            return this;
        }

        /// <summary>
        /// Gets the current meta title
        /// </summary>
        /// <returns>The meta title string</returns>
        public string GetMetaTitle() => _metaTitle;

        /// <summary>
        /// Gets the current meta description
        /// </summary>
        /// <returns>The meta description string</returns>
        public string GetMetaDescription() => _metaDescription;

        /// <summary>
        /// Gets the list of image URLs to be preloaded
        /// </summary>
        /// <returns>An immutable set of image URLs</returns>
        public IReadOnlySet<string> GetPreloadImageUrls() => _preloadImageUrls.ToFrozenSet();

        /// <summary>
        /// Gets whether the page should be indexed by search engines
        /// </summary>
        /// <returns>True if the page should not be indexed, false otherwise</returns>
        public bool IsNoIndex() => _noIndex;

        /// <summary>
        /// Gets the site name
        /// </summary>
        /// <returns>The name of the website</returns>
        public string GetSiteName() => _siteName;

        /// <summary>
        /// Gets the current locale
        /// </summary>
        /// <returns>The locale code (e.g., en-US)</returns>
        public string GetLocale() => _locale;

        /// <summary>
        /// Gets the canonical URL for the current page
        /// </summary>
        /// <returns>The canonical URL, or null if not set</returns>
        public Uri? GetCanonicalUrl() => _canonicalUrl;

        /// <summary>
        /// Gets the base URL of the website
        /// </summary>
        /// <returns>The base URL</returns>
        public Uri GetBaseUrl() => _baseUrl;

        /// <summary>
        /// Gets the current breadcrumb navigation trail
        /// </summary>
        /// <returns>An immutable list of breadcrumb links</returns>
        public IReadOnlyList<BreadcrumbLink> GetBreadCrumbs() => _breadcrumbs.AsReadOnly();
    }
}

