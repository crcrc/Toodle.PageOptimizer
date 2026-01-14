using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toodle.PageOptimizer.Models;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Toodle.PageOptimizer.Sitemap.Services;
using Toodle.PageOptimizer.Sitemap.Models;

namespace Toodle.PageOptimizer
{
    public class PageOptimizerOptions
    {
        public bool EnableHttpsCompression { get; set; } = false;
        public RequestCulture? UseRequestCulture { get; set; } = null;
        public bool ServeSitemap { get; set; } = false;
    }
    public static class PageOptimizerExtensions
    {
        public static IServiceCollection AddPageOptimizer(
            this IServiceCollection services,
            Action<PageOptimizerOptions> configureOptions = null)
        {

            services.AddSingleton<PageOptimizerOptions>();
            services.AddSingleton<PageOptimizerConfig>();
            services.AddScoped<IPageOptimizerService, PageOptimizerService>();

            var options = new PageOptimizerOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);

            if (options.ServeSitemap)
            {
                services.AddMemoryCache();
                services.AddSingleton<SitemapGenerator>();
                services.AddSingleton<SitemapService>();
            }

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

        public bool IsLocked => _isLocked;
        public void Lock() => _isLocked = true;
        public StaticFileCacheOptions? StaticFileCacheOptions => _staticFileCacheOptions;
        public SitemapOptions? SitemapOptions => _sitemapOptions;

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
                Paths = fileCacheOptions.Paths?.ToArray(),
                FileExtensions = fileCacheOptions.FileExtensions?.ToArray(),
                MaxAge = fileCacheOptions?.MaxAge,
                IsPublic = fileCacheOptions?.IsPublic
            };
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
    }
}
