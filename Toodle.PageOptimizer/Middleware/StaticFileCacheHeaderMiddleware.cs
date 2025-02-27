using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Toodle.PageOptimizer.Middleware
{
    public class StaticFileCacheHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public StaticFileCacheHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private static readonly string[] _defaultFileExtensions = new[] { ".js", ".css", ".ico" };
        private static readonly TimeSpan _defaultMaxAge = TimeSpan.FromDays(21);
        private static readonly bool _defaultIsPublic = true;

        public async Task InvokeAsync(HttpContext context, PageOptimizerConfig config)
        {
            var path = context.Request.Path.Value;
            if (string.IsNullOrEmpty(path)) return;

            TimeSpan maxAge = config?.StaticFileCacheOptions?.MaxAge ?? _defaultMaxAge;
            bool isPublic = config?.StaticFileCacheOptions?.IsPublic ?? _defaultIsPublic;
            string[] fileExtensions = config?.StaticFileCacheOptions?.FileExtensions ?? _defaultFileExtensions;
            string[] paths = config?.StaticFileCacheOptions?.Paths ?? Array.Empty<string>();

            // Normalize path for safer comparison
            path = path.TrimEnd('/');

            // Check path and extension match
            bool matchesPath = paths.Length == 0 || paths.Any(p =>
                path.StartsWith(p.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));

            bool matchesExtension = fileExtensions.Any(ext =>
                path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

            if (matchesPath && matchesExtension)
            {
                if (!context.Response.HasStarted && !context.Response.Headers.ContainsKey(HeaderNames.CacheControl))
                {
                    string cacheControl = isPublic ?
                        $"public, max-age={maxAge.TotalSeconds:0}" :
                        $"private, max-age={maxAge.TotalSeconds:0}";

                    context.Response.Headers[HeaderNames.CacheControl] = cacheControl;
                }
            }


            await _next(context);
        }
    }
}
