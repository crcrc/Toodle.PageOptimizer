using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Admirably.PageOptimizer.Sitemap.Middleware;
using Admirably.PageOptimizer.Rss.Middleware;
using Admirably.PageOptimizer.Rss.Services;

namespace Admirably.PageOptimizer.Middleware
{
    public static class PageOptimizerMiddlewareExtensions
    {
        /// <summary>
        /// Returns a fluent builder for configuring global site defaults such as base URL, title, default image, preloads, and sitemap.
        /// Must be called after app.Build() and before UsePageOptimizer().
        /// </summary>
        /// <param name="app">The IApplicationBuilder.</param>
        public static IPageOptimizerApp ConfigurePageOptimizer(this IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetService<PageOptimizerConfig>() ?? throw new InvalidOperationException("PageOptimizer services not found. Call services.AddPageOptimizer() before building the app.");
            var options = app.ApplicationServices.GetService<PageOptimizerOptions>() ?? throw new InvalidOperationException("PageOptimizer services not found. Call services.AddPageOptimizer() before building the app.");
            return new PageOptimizerApp(app, config, options);
        }

        /// <summary>
        /// Registers the PageOptimizer middleware pipeline. Locks configuration to prevent further changes.
        /// Must be called after ConfigurePageOptimizer().
        /// </summary>
        /// <param name="app">The IApplicationBuilder.</param>
        public static IApplicationBuilder UsePageOptimizer(this IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetService<PageOptimizerConfig>() ?? throw new InvalidOperationException("PageOptimizer services not found. Call services.AddPageOptimizer() before building the app.");
            var options = app.ApplicationServices.GetService<PageOptimizerOptions>() ?? throw new InvalidOperationException("PageOptimizer services not found. Call services.AddPageOptimizer() before building the app.");

            // Ensure config is locked before middleware is used
            config.Lock();

            app.UseMiddleware<AppendLinkHeaderMiddleware>();

            if (options.EnableHttpsCompression)
            {
                app.UseResponseCompression();
            }
            if (options.UseRequestCulture != null)
            {
                app.UseRequestLocalization();
            }

            if (config.StaticFileCacheOptions != null)
            {
                app.UseMiddleware<StaticFileCacheHeaderMiddleware>();
            }

            if (config.SitemapOptions != null)
            {
                app.UseMiddleware<SitemapMiddleware>();
            }

            if (config.RobotsTxtOptions != null)
            {
                app.UseMiddleware<RobotsTxtMiddleware>();
            }

            var rssFeedRegistrations = app.ApplicationServices.GetServices<RssFeedRegistration>();
            if (rssFeedRegistrations.Any())
            {
                if (string.IsNullOrWhiteSpace(config.BaseUrl))
                    throw new InvalidOperationException("AddRssFeed() requires WithBaseUrl() to be configured.");
                app.UseMiddleware<RssFeedMiddleware>();
            }

            return app;
        }
    }
}
