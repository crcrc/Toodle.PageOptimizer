using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Toodle.PageOptimizer.Middleware
{
    public class RobotsTxtMiddleware
    {
        private readonly RequestDelegate _next;
        private string? _cachedContent;

        public RobotsTxtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, PageOptimizerConfig config)
        {
            var path = config.RobotsTxtOptions?.Path;

            if (!string.IsNullOrWhiteSpace(path) && context.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                _cachedContent ??= GenerateContent(config);
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(_cachedContent);
                return;
            }

            await _next(context);
        }

        private static string GenerateContent(PageOptimizerConfig config)
        {
            var sb = new StringBuilder();

            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");

            if (config.RobotsTxtOptions?.AdditionalRules != null)
            {
                foreach (var line in config.RobotsTxtOptions.AdditionalRules)
                    sb.AppendLine(line);
            }

            if (config.SitemapOptions != null)
            {
                var sitemapUrl = config.BaseUrl.TrimEnd('/') + config.SitemapOptions.Path;
                sb.AppendLine();
                sb.AppendLine($"Sitemap: {sitemapUrl}");
            }

            return sb.ToString();
        }
    }
}
