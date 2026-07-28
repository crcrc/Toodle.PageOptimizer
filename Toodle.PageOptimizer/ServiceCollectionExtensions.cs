using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Admirably.PageOptimizer.Models;
using Admirably.PageOptimizer.Rss.Models;
using Admirably.PageOptimizer.Rss.Services;
using Admirably.PageOptimizer.Sitemap.Models;
using Admirably.PageOptimizer.Sitemap.Services;
using static Admirably.PageOptimizer.PageOptimizerConfig;

namespace Admirably.PageOptimizer
{
    public class PageOptimizerOptions
    {
        public bool EnableHttpsCompression { get; set; } = false;
        public RequestCulture? UseRequestCulture { get; set; } = null;
        public bool ServeSitemap { get; set; } = false;
    }
    public static class PageOptimizerExtensions
    {
        /// <summary>
        /// Registers PageOptimizer services. Call ConfigurePageOptimizer() and UsePageOptimizer() after app.Build() to complete setup.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureOptions">Optional action to enable compression and localization.</param>
        public static IServiceCollection AddPageOptimizer(
            this IServiceCollection services,
            Action<PageOptimizerOptions> configureOptions = null)
        {
            services.AddSingleton<PageOptimizerConfig>();
            services.AddScoped<IPageOptimizerService, PageOptimizerService>();
            services.AddMemoryCache();
            services.AddSingleton<RssFeedGenerator>();
            services.AddSingleton<RssFeedService>();

            var options = new PageOptimizerOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);

            services.AddSingleton<SitemapGenerator>();
            services.AddSingleton<SitemapService>();

            if (options.EnableHttpsCompression)
            {
                services.AddResponseCompression(compressionOptions =>
                {
                    compressionOptions.EnableForHttps = true;
                    compressionOptions.Providers.Add<BrotliCompressionProvider>();
                    compressionOptions.Providers.Add<GzipCompressionProvider>();
                    compressionOptions.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] {
                "image/svg+xml"
            });
                });

                services.Configure<BrotliCompressionProviderOptions>(providerOptions =>
                {
                    providerOptions.Level = CompressionLevel.Optimal;
                });

                services.Configure<GzipCompressionProviderOptions>(providerOptions =>
                {
                    providerOptions.Level = CompressionLevel.Optimal;
                });
            }

            if (options.UseRequestCulture != null)
            {
                services.Configure<RequestLocalizationOptions>(o =>
                {
                    var supportedCultures = new[] { options.UseRequestCulture.Culture };
                    o.DefaultRequestCulture = options.UseRequestCulture;
                    o.SupportedCultures = supportedCultures;
                    o.SupportedUICultures = supportedCultures;
                });
            }

            return services;
        }

        /// <summary>
        /// Registers a function that will be used as a sitemap source.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="urlProviderFunc">The function that returns a collection of sitemap URLs.</param>
        public static IServiceCollection AddSitemapSource(
            this IServiceCollection services,
            Func<IServiceProvider, Task<IEnumerable<SitemapUrl>>> urlProviderFunc)
        {
            // This line was causing the error because the class below was missing.
            services.AddScoped<ISitemapSource>(sp => new FuncSitemapSource(urlProviderFunc, sp));
            return services;
        }

        /// <summary>
        /// Registers a class that implements ISitemapSource.
        /// </summary>
        public static IServiceCollection AddSitemapSource<T>(this IServiceCollection services)
            where T : class, ISitemapSource
        {
            return services.AddScoped<ISitemapSource, T>();
        }

        /// <summary>
        /// Registers an RSS feed to be served at the specified path.
        /// Multiple feeds can be registered at different paths.
        /// The feed is cached for the specified duration. Item limiting is the caller's responsibility.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="path">The path at which to serve the feed (e.g. "/feed.xml").</param>
        /// <param name="title">The RSS channel title.</param>
        /// <param name="description">The RSS channel description.</param>
        /// <param name="source">A function that returns the feed items.</param>
        /// <param name="cacheDuration">How long to cache the generated feed. Defaults to 2 hours.</param>
        public static IServiceCollection AddRssFeed(
            this IServiceCollection services,
            string path,
            string title,
            string description,
            Func<IServiceProvider, Task<IEnumerable<RssItem>>> source,
            TimeSpan? cacheDuration = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Feed path cannot be empty.", nameof(path));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Feed title cannot be empty.", nameof(title));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Feed description cannot be empty.", nameof(description));
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            services.AddSingleton(new RssFeedRegistration
            {
                Path = PathValidation.NormalizePath(path, nameof(path)),
                Title = title,
                Description = description,
                CacheDuration = cacheDuration ?? TimeSpan.FromHours(2),
                Source = source
            });

            return services;
        }


        /// <summary>
        /// An internal adapter class that wraps a user-provided function into an ISitemapSource.
        /// This allows for simple, inline registration of sitemap sources.
        /// </summary>
        private class FuncSitemapSource : ISitemapSource
        {
            private readonly Func<IServiceProvider, Task<IEnumerable<SitemapUrl>>> _urlProviderFunc;
            private readonly IServiceProvider _serviceProvider;

            public FuncSitemapSource(
                Func<IServiceProvider, Task<IEnumerable<SitemapUrl>>> urlProviderFunc,
                IServiceProvider serviceProvider)
            {
                _urlProviderFunc = urlProviderFunc ?? throw new ArgumentNullException(nameof(urlProviderFunc));
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }

            /// <summary>
            /// Executes the user's function to get the sitemap URLs.
            /// </summary>
            public Task<IEnumerable<SitemapUrl>> GetUrlsAsync()
            {
                // When this is called by the SitemapService, it will invoke the
                // lambda expression you defined in Program.cs.
                return _urlProviderFunc(_serviceProvider);
            }
        }
    }

    public class PageOptimizerConfig
    {
        private bool _isLocked = false;
        private string _siteName = string.Empty;
        private string _titleSeparator = string.Empty;
        private string _baseUrl = string.Empty;
        private string _locale = "en-US";
        private readonly List<(string Domain, bool CrossOrigin)> _preconnectDomains = new List<(string Domain, bool CrossOrigin)>();
        private readonly List<(string Url, AssetType AssetType, bool CrossOrigin)> _preloadResources = new List<(string Url, AssetType AssetType, bool CrossOrigin)>();
        private readonly List<(string Title, string Url)> _defaultBreadcrumbs = new List<(string Title, string Url)>();
        private StaticFileCacheOptions? _staticFileCacheOptions;
        private SitemapOptions? _sitemapOptions;
        private RobotsTxtOptions? _robotsTxtOptions;
        private string? _defaultImage;

        public bool IsLocked => _isLocked;
        public void Lock() => _isLocked = true;
        public StaticFileCacheOptions? StaticFileCacheOptions => _staticFileCacheOptions;
        public SitemapOptions? SitemapOptions => _sitemapOptions;
        public RobotsTxtOptions? RobotsTxtOptions => _robotsTxtOptions;
        public string? DefaultImage => _defaultImage;

        private void EnsureNotLocked()
        {
            if (_isLocked)
                throw new InvalidOperationException("Configuration cannot be modified after application has started.");
        }

        public string SiteName
        {
            get => _siteName;
            set
            {
                EnsureNotLocked();
                _siteName = value;
            }
        }

        public string TitleSeparator
        {
            get => _titleSeparator;
            set
            {
                EnsureNotLocked();
                _titleSeparator = value;
            }
        }

        public string BaseUrl
        {
            get => _baseUrl;
            set
            {
                EnsureNotLocked();
                _baseUrl = value;
            }
        }

        public string Locale
        {
            get => _locale;
            set
            {
                EnsureNotLocked();
                _locale = value;
            }
        }

        public IReadOnlyList<(string Domain, bool CrossOrigin)> PreconnectDomains => _preconnectDomains.AsReadOnly();

        public void AddPreconnectDomain(string domain, bool crossOrigin)
        {
            EnsureNotLocked();
            _preconnectDomains.Add((domain, crossOrigin));
        }

        public IReadOnlyList<(string Url, AssetType AssetType, bool CrossOrigin)> PreloadResources => _preloadResources.AsReadOnly();

        public void AddPreloadResource(string url, AssetType assetType, bool crossOrigin)
        {
            EnsureNotLocked();
            _preloadResources.Add((url, assetType, crossOrigin));
        }

        public IReadOnlyList<(string Title, string Url)> DefaultBreadcrumbs => _defaultBreadcrumbs.AsReadOnly();

        public void AddDefaultBreadcrumb(string title, string url)
        {
            EnsureNotLocked();
            _defaultBreadcrumbs.Add((title, url));
        }

        public void AddStaticFileCacheOptions(StaticFileCacheOptions fileCacheOptions)
        {
            EnsureNotLocked();

            _staticFileCacheOptions = new StaticFileCacheOptions
            {
                Paths = fileCacheOptions.Paths?.Select(p => PathValidation.NormalizePath(p, nameof(fileCacheOptions.Paths))).ToArray(),
                FileExtensions = fileCacheOptions.FileExtensions?.ToArray(),
                MaxAge = fileCacheOptions?.MaxAge,
                IsPublic = fileCacheOptions?.IsPublic
            };
        }

        public void SetRobotsTxtOptions(RobotsTxtOptions options)
        {
            EnsureNotLocked();
            _robotsTxtOptions = options;
        }

        public void SetDefaultImage(string imageUrl)
        {
            EnsureNotLocked();
            _defaultImage = imageUrl;
        }

        public void AddSitemapOptions(SitemapOptions sitemapOptions)
        {
            EnsureNotLocked();

            _sitemapOptions = new SitemapOptions
            {
                CacheDuration = sitemapOptions.CacheDuration,
                Path = sitemapOptions.Path
            };
        }

        internal static class PathValidation
        {
            public static string NormalizePath(string path, string paramName)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("Path cannot be empty.", paramName);

                return path.StartsWith('/') ? path : "/" + path;
            }
        }
    }
}
