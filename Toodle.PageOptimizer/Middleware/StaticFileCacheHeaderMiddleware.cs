using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Admirably.PageOptimizer.Middleware
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
            var requestPath = context.Request.Path;
            if (!requestPath.HasValue)
            {
                await _next(context);
                return;
            }

            TimeSpan maxAge = config?.StaticFileCacheOptions?.MaxAge ?? _defaultMaxAge;
            bool isPublic = config?.StaticFileCacheOptions?.IsPublic ?? _defaultIsPublic;
            string[] fileExtensions = config?.StaticFileCacheOptions?.FileExtensions ?? _defaultFileExtensions;
            string[] paths = config?.StaticFileCacheOptions?.Paths ?? Array.Empty<string>();

            bool matchesPath = paths.Length == 0 || paths.Any(p =>
                requestPath.StartsWithSegments(new PathString(p.TrimEnd('/')), StringComparison.OrdinalIgnoreCase, out _));

            bool matchesExtension = fileExtensions.Any(ext =>
                requestPath.Value!.TrimEnd('/').EndsWith(ext, StringComparison.OrdinalIgnoreCase));


            if (matchesPath && matchesExtension)
            {
                string cacheControl = isPublic ?
                    $"public, max-age={maxAge.TotalSeconds:0}" :
                    $"private, max-age={maxAge.TotalSeconds:0}";

                context.Response.OnStarting(() =>
                {
                    var status = context.Response.StatusCode;
                    if (status == StatusCodes.Status200OK
                        || status == StatusCodes.Status304NotModified
                        || status == StatusCodes.Status206PartialContent)
                    {
                        context.Response.Headers[HeaderNames.CacheControl] = cacheControl;
                    }
                    return Task.CompletedTask;
                });
            }

            await _next(context);
        }
    }
}
