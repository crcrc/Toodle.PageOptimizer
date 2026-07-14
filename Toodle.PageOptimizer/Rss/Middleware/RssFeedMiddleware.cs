using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Toodle.PageOptimizer.Rss.Services;

namespace Toodle.PageOptimizer.Rss.Middleware
{
    public class RssFeedMiddleware
    {
        private readonly RequestDelegate _next;

        public RssFeedMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, RssFeedService rssFeedService)
        {
            var path = context.Request.Path.Value;

            if (!string.IsNullOrWhiteSpace(path) && rssFeedService.HasFeed(path))
            {
                var xml = await rssFeedService.GetFeedXmlAsync(path);

                if (xml != null)
                {
                    context.Response.ContentType = "application/rss+xml; charset=utf-8";
                    await context.Response.WriteAsync(xml);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
                return;
            }

            await _next(context);
        }
    }
}
