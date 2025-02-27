using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toodle.PageOptimizer.Models;

namespace Toodle.PageOptimizer
{
    public interface IPageOptimizerApp
    {
        IPageOptimizerApp WithBaseTitle(string siteName, string separator = "|");
        IPageOptimizerApp WithBaseUrl(string baseUrl);
        IPageOptimizerApp AddDefaultPreconnect(string domain, bool crossOrigin = true);
        IPageOptimizerApp AddDefaultPreload(string url, AssetType assetType, bool crossOrigin = false);
        IPageOptimizerApp AddDefaultBreadcrumb(string title, string url = "");
        IPageOptimizerApp AddStaticFileCacheHeaders(Action<StaticFileCacheOptions> configure = null);
    }

    public class StaticFileCacheOptions
    {
        public string[]? Paths { get; set; }
        public string[]? FileExtensions { get; set; }
        public TimeSpan? MaxAge { get; set; }
        public bool? IsPublic { get; set; }
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
