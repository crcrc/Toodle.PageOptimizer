using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Toodle.PageOptimizer.Middleware
{
    public static class PageOptimizerMiddlewareExtensions
    {
        public static IPageOptimizerApp UsePageOptimizer(this IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetService<PageOptimizerConfig>();
            var options = app.ApplicationServices.GetService<PageOptimizerOptions>();

            var pageOptimizerApp = new PageOptimizerApp(app, config, options);

            app.UseMiddleware<PageOptimizerMiddleware>();

            if (options.EnableHttpsCompression)
            {
                app.UseResponseCompression();
            }

            if (options.UseRequestCulture != null)
            {
                app.UseRequestLocalization();
            }


            return pageOptimizerApp;
        }

        //public static IPageOptimizerApp Start(this IPageOptimizerApp pageOptimizerApp)
        //{
        //    pageOptimizerApp.Start();

        //    return pageOptimizerApp;
        //}
    }

    public class PageOptimizerMiddleware
    {
        private readonly RequestDelegate _next;

        public PageOptimizerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private static readonly Regex _fileExtensionRegex = new Regex(@"\.[a-zA-Z0-9]+$", RegexOptions.Compiled);
        public async Task InvokeAsync(HttpContext context, IPageOptimizerService pageOptimizerService)
        {
            if (context.Request.Method == "GET"
                &&!context.Request.Headers.ContainsKey("X-Requested-With")
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
