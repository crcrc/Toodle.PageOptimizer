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

namespace Toodle.PageOptimizer
{
    public class PageOptimizerOptions
    {
        public bool EnableHttpsCompression { get; set; } = false;
        public RequestCulture? UseRequestCulture { get; set; } = null;
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

            //app.UseStaticFiles(new StaticFileOptions
            //{
            //    OnPrepareResponse = ctx =>
            //    {
            //        var path = ctx.File.PhysicalPath;

            //        // 7 days public cache for self hosted files
            //        if (path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            //            path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            //            path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
            //            path.EndsWith("kameleoon_shearings.html", StringComparison.OrdinalIgnoreCase))
            //        {
            //            var maxAge = TimeSpan.FromDays(7);
            //            ctx.Context.Response.Headers[HeaderNames.CacheControl] =
            //                $"public, max-age={maxAge.TotalSeconds:0}";
            //        }
            //    }
            //});
            return services;
        }

        public static IServiceCollection UseHttpsResponseCompression(this IServiceCollection services)
        {
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] {
                    "image/svg+xml" 
                });
            });

            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });
            return services;
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

        public bool IsLocked => _isLocked;

        public void Lock() => _isLocked = true;

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
    }
}
