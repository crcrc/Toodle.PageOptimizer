using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using Toodle.PageOptimizer.Models;

namespace Toodle.PageOptimizer
{
    public interface IPageOptimizerService
    {
        /// <summary>Returns the meta title set for the current request.</summary>
        public string GetMetaTitle();

        /// <summary>Returns the meta description set for the current request.</summary>
        public string GetMetaDescription();

        /// <summary>Returns true if the robots directives include noindex.</summary>
        public bool IsNoIndex();

        /// <summary>Returns the full robots directives string, or null if not set.</summary>
        public string? GetRobots();

        /// <summary>Returns the global site name configured at startup.</summary>
        public string GetSiteName();

        /// <summary>Returns the title separator configured at startup (e.g. "|").</summary>
        public string GetTitleSeparator();

        /// <summary>Returns the locale for the current request (e.g. "en-US").</summary>
        public string GetLocale();

        /// <summary>Returns the canonical URL set for the current request, or null if not set.</summary>
        public Uri? GetCanonicalUrl();

        /// <summary>Returns the base URL configured at startup.</summary>
        public Uri GetBaseUrl();

        /// <summary>Returns the breadcrumb list for the current request, including any global defaults.</summary>
        public IReadOnlyList<(string Title, string Url)> GetBreadCrumbs();

        /// <summary>Returns the OgType object set for the current request, or null if not set.</summary>
        public OgType? GetOgType();

        /// <summary>Returns the Twitter Card type set for the current request, or null if not set.</summary>
        public TwitterCard? GetTwitterCard();

        /// <summary>Returns the absolute image URL set for the current request, or null if not set.</summary>
        public string? GetMetaImage();

        /// <summary>Returns the image width in pixels, or null if not set.</summary>
        public int? GetMetaImageWidth();

        /// <summary>Returns the image height in pixels, or null if not set.</summary>
        public int? GetMetaImageHeight();

        /// <summary>Returns the image alt text, or null if not set.</summary>
        public string? GetMetaImageAlt();

        /// <summary>
        /// Adds a rel=preconnect hint for the given domain to the HTTP Link header.
        /// </summary>
        /// <param name="domain">The absolute URL of the domain to preconnect to.</param>
        /// <param name="crossOrigin">Whether to include the crossorigin attribute. Defaults to true.</param>
        public IPageOptimizerService AddPreconnect(string domain, bool crossOrigin = true);

        /// <summary>
        /// Adds a rel=preload hint for the given resource to the HTTP Link header.
        /// </summary>
        /// <param name="url">The URL of the resource to preload.</param>
        /// <param name="AssetType">The type of asset (e.g. Script, Style, Font).</param>
        /// <param name="crossOrigin">Whether to include the crossorigin attribute. Defaults to false.</param>
        public IPageOptimizerService AddPreload(string url, AssetType AssetType, bool crossOrigin = false);

        /// <summary>
        /// Sets the page title. Rendered as "{title} {separator} {siteName}" in the tag helper.
        /// </summary>
        /// <param name="title">The page-specific title.</param>
        public IPageOptimizerService SetMetaTitle(string title);

        /// <summary>
        /// Overrides the locale for the current request, used for the og:locale meta tag.
        /// </summary>
        /// <param name="locale">A locale string such as "en-GB" or "fr-FR".</param>
        public IPageOptimizerService SetLocale(string locale);

        /// <summary>
        /// Sets the meta description for the current page. Used for the description, og:description, and twitter:description tags.
        /// </summary>
        /// <param name="description">The page description.</param>
        public IPageOptimizerService SetMetaDescription(string description);

        /// <summary>
        /// Shortcut for SetRobots("noindex"). Prevents search engine indexing of the current page.
        /// </summary>
        public IPageOptimizerService SetNoIndex();

        /// <summary>
        /// Sets the full robots meta tag directives for the current page.
        /// Accepts any valid robots string (e.g. "noindex, nofollow", "noarchive").
        /// </summary>
        /// <param name="directives">A comma-separated string of robots directives.</param>
        public IPageOptimizerService SetRobots(string directives);

        /// <summary>
        /// Sets the canonical URL for the current page. Accepts a relative path (resolved against the base URL) or an absolute URL.
        /// </summary>
        /// <param name="url">A relative path (e.g. "/products/slug") or absolute URL.</param>
        public IPageOptimizerService SetCanonicalUrl(string url);

        /// <summary>
        /// Sets the og:type for the current page. Use a typed object (OgTypeArticle, OgTypeProduct, etc.)
        /// to also render type-specific meta tags. Not rendered unless explicitly set.
        /// </summary>
        /// <param name="ogType">An OgType instance such as OgTypeArticle or OgTypeWebsite.</param>
        public IPageOptimizerService SetOgType(OgType ogType);

        /// <summary>
        /// Sets the twitter:card meta tag for the current page.
        /// Not rendered unless explicitly set.
        /// </summary>
        /// <param name="twitterCard">The Twitter Card type to use.</param>
        public IPageOptimizerService SetTwitterCard(TwitterCard twitterCard);

        /// <summary>
        /// Sets the share image for the current page, overriding the global default.
        /// Accepts a relative path (resolved against the base URL) or an absolute URL.
        /// Used for og:image and twitter:image tags. Width, height, and alt are optional
        /// but recommended — social platforms use them to avoid re-fetching the image.
        /// </summary>
        /// <param name="url">A relative path or absolute URL to the image.</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="alt">Alt text for the image, used for og:image:alt and twitter:image:alt.</param>
        public IPageOptimizerService SetMetaImage(string url, int? width = null, int? height = null, string? alt = null);

        /// <summary>
        /// Appends a breadcrumb to the end of the breadcrumb list for the current request.
        /// </summary>
        /// <param name="title">The display label for the breadcrumb.</param>
        /// <param name="url">The URL for the breadcrumb. Leave empty for the current (last) page.</param>
        public IPageOptimizerService AddBreadCrumb(string title, string url);

        /// <summary>
        /// Removes all breadcrumbs, including any global defaults, for the current request.
        /// </summary>
        public IPageOptimizerService ClearBreadcrumbs();

        /// <summary>
        /// Writes all configured preconnect and preload hints to the HTTP Link response header.
        /// Called automatically by the middleware.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        public void AddLinkHeaders(HttpContext context);
    }

    public class PageOptimizerService : IPageOptimizerService
    {
        private readonly PageOptimizerConfig _config;

        private readonly string _siteName;
        private readonly string _titleSeparator;
        private readonly Uri _baseUrl;
        private readonly List<(string Domain, bool CrossOrigin)> _preconnectDomains = new List<(string Domain, bool CrossOrigin)>();
        private readonly List<(string Url, AssetType AssetType, bool CrossOrigin)> _preloadResources = new List<(string Url, AssetType AssetType, bool CrossOrigin)>();
        private string _metaTitle = string.Empty;
        private Uri? _canonicalUrl;
        private string _metaDescription = string.Empty;
        private string? _robots;
        private string _locale;
        private OgType? _ogType;
        private TwitterCard? _twitterCard;
        private string? _metaImage;
        private int? _metaImageWidth;
        private int? _metaImageHeight;
        private string? _metaImageAlt;
        private readonly List<(string Title, string Url)> _breadcrumbs = new List<(string Title, string Url)>();

        public string GetMetaTitle() => _metaTitle;
        public string GetMetaDescription() => _metaDescription;
        public bool IsNoIndex() => _robots?.Contains("noindex", StringComparison.OrdinalIgnoreCase) ?? false;
        public string? GetRobots() => _robots;
        public string GetSiteName() => _siteName;
        public string GetTitleSeparator() => _titleSeparator;
        public string GetLocale() => _locale;
        public Uri? GetCanonicalUrl() => _canonicalUrl;
        public Uri GetBaseUrl() => _baseUrl;
        public OgType? GetOgType() => _ogType;
        public TwitterCard? GetTwitterCard() => _twitterCard;
        public string? GetMetaImage() => _metaImage;
        public int? GetMetaImageWidth() => _metaImageWidth;
        public int? GetMetaImageHeight() => _metaImageHeight;
        public string? GetMetaImageAlt() => _metaImageAlt;
        public IReadOnlyList<(string Title, string Url)> GetBreadCrumbs() => _breadcrumbs.AsReadOnly();

        public PageOptimizerService(PageOptimizerConfig config)
        {
            _config = config;

            if (!string.IsNullOrWhiteSpace(_config.SiteName))
                _siteName = _config.SiteName;

            if (!string.IsNullOrWhiteSpace(_config.TitleSeparator))
                _titleSeparator = _config.TitleSeparator;

            if (!string.IsNullOrWhiteSpace(_config.BaseUrl))
                _baseUrl = new Uri(_config.BaseUrl);

            if (_config.PreconnectDomains != null)
                _preconnectDomains.AddRange(_config.PreconnectDomains);

            if (_config.PreloadResources != null)
                _preloadResources.AddRange(_config.PreloadResources);

            if (_config.DefaultBreadcrumbs != null)
                _breadcrumbs.AddRange(_config.DefaultBreadcrumbs);

            if (_config.DefaultImage != null)
                _metaImage = _config.DefaultImage;
        }

        public IPageOptimizerService AddPreconnect(string domain, bool crossOrigin = true)
        {
            _preconnectDomains.Add((domain, crossOrigin));
            return this;
        }

        public IPageOptimizerService AddPreload(string url, AssetType AssetType, bool crossOrigin = false)
        {
            _preloadResources.Add((url, AssetType, crossOrigin));
            return this;
        }

        public void AddLinkHeaders(HttpContext context)
        {
            if (_preconnectDomains.Count == 0 && _preloadResources.Count == 0)
                return;

            var linkValues = new List<string>();

            // Add preconnect links
            foreach (var (domain, crossOrigin) in _preconnectDomains)
            {
                linkValues.Add(crossOrigin
                    ? $"<{domain}>; rel=preconnect; crossorigin"
                    : $"<{domain}>; rel=preconnect");
            }

            // Add preload links
            foreach (var (url, type, crossOrigin) in _preloadResources)
            {
                var asValue = type.ToString().ToLowerInvariant();
                linkValues.Add(crossOrigin
                    ? $"<{url}>; rel=preload; as={asValue}; crossorigin"
                    : $"<{url}>; rel=preload; as={asValue}");
            }

            if (linkValues.Count > 0)
                context.Response.Headers.AppendCommaSeparatedValues("Link", linkValues.ToArray());
        }

        public IPageOptimizerService SetMetaTitle(string title)
        {
            _metaTitle = title;
            return this;
        }

        public IPageOptimizerService SetLocale(string locale)
        {
            _locale = locale;
            return this;
        }

        public IPageOptimizerService SetMetaDescription(string description)
        {
            _metaDescription = description;
            return this;
        }

        public IPageOptimizerService SetNoIndex() => SetRobots("noindex");

        public IPageOptimizerService SetRobots(string directives)
        {
            if (string.IsNullOrWhiteSpace(directives))
                throw new ArgumentException("Robots directives cannot be empty", nameof(directives));

            _robots = directives;
            return this;
        }

        public IPageOptimizerService SetCanonicalUrl(string url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            _canonicalUrl = new Uri(PageOptimizerApp.ResolveImageUrl(url, _config.BaseUrl));
            return this;
        }

        public IPageOptimizerService SetMetaImage(string url, int? width = null, int? height = null, string? alt = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Image URL cannot be empty", nameof(url));

            _metaImage = PageOptimizerApp.ResolveImageUrl(url, _config.BaseUrl);
            _metaImageWidth = width;
            _metaImageHeight = height;
            _metaImageAlt = alt;
            return this;
        }

        public IPageOptimizerService SetOgType(OgType ogType)
        {
            _ogType = ogType;
            return this;
        }

        public IPageOptimizerService SetTwitterCard(TwitterCard twitterCard)
        {
            _twitterCard = twitterCard;
            return this;
        }

        public IPageOptimizerService AddBreadCrumb(string title, string url = "")
        {
            _breadcrumbs.Add((title, url));
            return this;
        }

        public IPageOptimizerService ClearBreadcrumbs()
        {
            _breadcrumbs.Clear();
            return this;
        }


    }


}

