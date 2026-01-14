using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Toodle.PageOptimizer.Sitemap.Middleware;

namespace Toodle.PageOptimizer.Middleware
{
    public static class PageOptimizerMiddlewareExtensions
    {
        public static IPageOptimizerApp ConfigurePageOptimizer(this IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetService<PageOptimizerConfig>();
            var options = app.ApplicationServices.GetService<PageOptimizerOptions>();
            return new PageOptimizerApp(app, config, options);
        }


        public static IApplicationBuilder UsePageOptimizer(this IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetService<PageOptimizerConfig>();
            var options = app.ApplicationServices.GetService<PageOptimizerOptions>();

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

            if (options.ServeSitemap)
            {
                app.UseMiddleware<SitemapMiddleware>();
            }

            return app;
        }
    }
}
