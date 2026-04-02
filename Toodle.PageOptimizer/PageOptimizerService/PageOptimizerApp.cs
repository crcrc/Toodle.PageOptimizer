using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toodle.PageOptimizer.Models;
using Toodle.PageOptimizer.Sitemap.Middleware;

namespace Toodle.PageOptimizer
{
    public interface IPageOptimizerApp
    {
        /// <summary>
        /// Sets the global site name and title separator used when rendering page titles.
        /// The title is rendered as "{page title} {separator} {site name}".
        /// </summary>
        /// <param name="siteName">The name of the site (e.g. "My Site").</param>
        /// <param name="separator">The separator between page title and site name. Defaults to "|".</param>
        IPageOptimizerApp WithBaseTitle(string siteName, string separator = "|");

        /// <summary>
        /// Sets the base URL of the site. Required for resolving relative canonical URLs, image URLs, and breadcrumb links.
        /// </summary>
        /// <param name="baseUrl">The absolute base URL (e.g. "https://www.example.com").</param>
        IPageOptimizerApp WithBaseUrl(string baseUrl);

        /// <summary>
        /// Sets the global default share image applied to every page unless overridden by SetMetaImage().
        /// Accepts a relative path (resolved against the base URL) or an absolute URL.
        /// </summary>
        /// <param name="url">A relative path or absolute URL to the image.</param>
        IPageOptimizerApp WithDefaultImage(string url);

        /// <summary>
        /// Adds a rel=preconnect hint for the given domain to every HTML response's HTTP Link header.
        /// </summary>
        /// <param name="domain">The absolute URL of the domain to preconnect to.</param>
        /// <param name="crossOrigin">Whether to include the crossorigin attribute. Defaults to true.</param>
        IPageOptimizerApp AddDefaultPreconnect(string domain, bool crossOrigin = true);

        /// <summary>
        /// Adds a rel=preload hint for the given resource to every HTML response's HTTP Link header.
        /// </summary>
        /// <param name="url">The relative or absolute URL of the resource to preload.</param>
        /// <param name="assetType">The type of asset (e.g. Script, Style, Font).</param>
        /// <param name="crossOrigin">Whether to include the crossorigin attribute. Defaults to false.</param>
        IPageOptimizerApp AddDefaultPreload(string url, AssetType assetType, bool crossOrigin = false);

        /// <summary>
        /// Adds a breadcrumb to the global default list prepended to every page's breadcrumb trail.
        /// </summary>
        /// <param name="title">The display label for the breadcrumb.</param>
        /// <param name="url">The URL for the breadcrumb. Leave empty for a label-only entry.</param>
        IPageOptimizerApp AddDefaultBreadcrumb(string title, string url = "");

        /// <summary>
        /// Enables Cache-Control header injection for static file responses matching the configured extensions and paths.
        /// </summary>
        /// <param name="configure">Optional action to configure paths, extensions, max-age, and public/private.</param>
        IPageOptimizerApp AddStaticFileCacheHeaders(Action<StaticFileCacheOptions> configure = null);

        /// <summary>
        /// Enables sitemap.xml generation and serving. Requires WithBaseUrl() to have been called first.
        /// Sitemap sources must be registered via AddSitemapSource() in AddPageOptimizer().
        /// </summary>
        /// <param name="configure">Optional action to configure the sitemap path and cache duration.</param>
        IPageOptimizerApp ServeSitemap(Action<SitemapOptions> configure = null);
    }

    public class StaticFileCacheOptions
    {
        public string[]? Paths { get; set; }
        public string[]? FileExtensions { get; set; }
        public TimeSpan? MaxAge { get; set; }
        public bool? IsPublic { get; set; }
    }

    public class SitemapOptions
    {
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(4);
        public string Path { get; set; } = "/sitemap.xml";
    }


    public class PageOptimizerApp : IPageOptimizerApp
    {
        private readonly IApplicationBuilder _app;
        private readonly PageOptimizerConfig _config;
        private readonly PageOptimizerOptions _options;

        public PageOptimizerApp(IApplicationBuilder app, PageOptimizerConfig config, PageOptimizerOptions options)
        {
            _app = app;
            _options = options;
            _config = config;

            if (_options.UseRequestCulture != null)
            {
                _config.Locale = _options.UseRequestCulture.ToString();
            }
        }

        public IPageOptimizerApp AddStaticFileCacheHeaders(Action<StaticFileCacheOptions> configure = null)
        {
            var options = new StaticFileCacheOptions();
            configure?.Invoke(options);

           _config.AddStaticFileCacheOptions(options);

            return this;
        }


        public IPageOptimizerApp WithBaseTitle(string siteName, string separator = "|")
        {
            EnsureConfigNotLocked();

            _config.SiteName = siteName;
            _config.TitleSeparator = separator;
            return this;
        }

        public IPageOptimizerApp WithBaseUrl(string baseUrl)
        {
            EnsureConfigNotLocked();

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL cannot be empty", nameof(baseUrl));

            if (!IsValidAbsoluteUrl(baseUrl))
                throw new ArgumentException("Invalid URL format", nameof(baseUrl));

            _config.BaseUrl = baseUrl.TrimEnd('/');
            return this;
        }

        public IPageOptimizerApp AddDefaultPreconnect(string domain, bool crossOrigin = true)
        {
            EnsureConfigNotLocked();

            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Domain cannot be empty", nameof(domain));

            if (!IsValidAbsoluteUrl(domain))
                throw new ArgumentException("Invalid domain format. Must be absolute URL.", nameof(domain));


            _config.AddPreconnectDomain(domain, crossOrigin);
            return this;
        }

        public IPageOptimizerApp AddDefaultPreload(string url, AssetType assetType, bool crossOrigin = false)
        {
            EnsureConfigNotLocked();

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be empty", nameof(url));

            if (!IsValidRelativeUrl(url))
                throw new ArgumentException("Invalid URL format", nameof(url));

            if (!Enum.IsDefined(typeof(AssetType), assetType))
                throw new ArgumentException("Invalid asset type", nameof(assetType));

            _config.AddPreloadResource(url, assetType, crossOrigin);
            return this;
        }

        public IPageOptimizerApp AddDefaultBreadcrumb(string title, string url = "")
        {
            EnsureConfigNotLocked();

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Breadcrumb title cannot be empty", nameof(title));

            if (!string.IsNullOrWhiteSpace(url) && !IsValidRelativeUrl(url) && !url.StartsWith("/"))
                throw new ArgumentException("Invalid URL format. Must be absolute URL or start with '/'", nameof(url));

            _config.AddDefaultBreadcrumb(title, url);
            return this;
        }

        public IPageOptimizerApp WithDefaultImage(string url)
        {
            EnsureConfigNotLocked();

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Image URL cannot be empty", nameof(url));

            _config.SetDefaultImage(ResolveImageUrl(url, _config.BaseUrl));
            return this;
        }

        public IPageOptimizerApp ServeSitemap(Action<SitemapOptions> configure = null)
        {
            EnsureConfigNotLocked();

            if (string.IsNullOrWhiteSpace(_config.BaseUrl))
                throw new ArgumentException("WithBaseUrl() must be called before ServeSitemap() to set the Base Url", nameof(_config.BaseUrl));

            var options = new SitemapOptions();
            configure?.Invoke(options);

            _config.AddSitemapOptions(options);

            return this;
        }


        internal static string ResolveImageUrl(string url, string baseUrl)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri) &&
                (absoluteUri.Scheme == "http" || absoluteUri.Scheme == "https"))
                return url;

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("WithBaseUrl() must be called before setting a relative image URL.");

            return new Uri(new Uri(baseUrl), url).ToString();
        }

        private static bool IsValidAbsoluteUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == "http" || uri.Scheme == "https");
        }

        // For URLs that can be either absolute or relative (like breadcrumbs)
        private static bool IsValidRelativeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true; // Empty URL is valid for some contexts

            return Uri.TryCreate(url, UriKind.Absolute, out _) ||
                   (url.StartsWith("/") && Uri.TryCreate("http://example.com" + url, UriKind.Absolute, out _));
        }

        private void EnsureConfigNotLocked()
        {
            if (_config.IsLocked)
                throw new InvalidOperationException("Configuration cannot be modified after application has started.");
        }
    }
}
