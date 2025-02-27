using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Toodle.PageOptimizer.Middleware
{
    public class AppendLinkHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public AppendLinkHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private static readonly Regex _fileExtensionRegex = new Regex(@"\.(html|htm|css|js|jpg|jpeg|png|gif|ico|pdf|svg|webp|mp3|mp4|webm|zip|rar|txt|xml|json)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public async Task InvokeAsync(HttpContext context, IPageOptimizerService pageOptimizerService)
        {
            if (context.Request.Method == "GET"
                && !context.Request.Headers.ContainsKey("X-Requested-With")
                && context.Request.Headers.Accept.ToString().Contains("text/html")
                && !context.Response.HasStarted
                && !_fileExtensionRegex.IsMatch(context.Request.Path.Value))
            {
                pageOptimizerService.AddLinkHeaders(context);
            }

            await _next(context);
        }
    }
}
