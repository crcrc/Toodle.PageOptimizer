using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Toodle.PageOptimizer.Middleware
{
    public class RobotsTxtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Lazy<string> _cachedContent;
        private readonly PageOptimizerConfig _config;

        public RobotsTxtMiddleware(RequestDelegate next, PageOptimizerConfig config)
        {
            _next = next;
            _config = config;
            _cachedContent = new Lazy<string>(() => GenerateContent(config), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public async Task InvokeAsync(HttpContext context, PageOptimizerConfig config)
        {
            var path = config.RobotsTxtOptions?.Path;

            if (!string.IsNullOrWhiteSpace(path) && context.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(_cachedContent.Value);
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
