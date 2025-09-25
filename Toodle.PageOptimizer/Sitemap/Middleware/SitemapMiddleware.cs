using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toodle.PageOptimizer.Sitemap.Services;

namespace Toodle.PageOptimizer.Sitemap.Middleware
{
    public class SitemapMiddleware
    {
        private readonly RequestDelegate _next;

        public SitemapMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, SitemapService sitemapService)
        {
            if (context.Request.Path.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase))
            {
                string? sitemapContent = await sitemapService.GetSitemapXmlAsync();

                if (sitemapContent is not null)
                {
                    context.Response.ContentType = "application/xml";
                    await context.Response.WriteAsync(sitemapContent);
                }
                else
                {
                    // This might happen on the very first request if the refresh fails.
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
                return; // End the request.
            }

            await _next(context);
        }
    }
}
