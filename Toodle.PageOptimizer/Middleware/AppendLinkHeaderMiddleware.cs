using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Admirably.PageOptimizer.Middleware
{
    public class AppendLinkHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public AppendLinkHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private static readonly Regex _fileExtensionRegex = new Regex(@"\.(html|htm|css|js|jpg|jpeg|png|gif|ico|pdf|svg|webp|mp3|mp4|webm|zip|rar|txt|xml|json)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Pipeline re-execution (UseExceptionHandler / UseStatusCodePagesWithReExecute)
        // runs this middleware twice on the same HttpContext; without this guard both
        // passes register OnStarting and every link-value is emitted twice.
        private const string _registeredKey = "Admirably.PageOptimizer.LinkHeaderRegistered";

        public async Task InvokeAsync(HttpContext context, IPageOptimizerService pageOptimizerService)
        {
            if (context.Request.Method == "GET"
                && !context.Request.Headers.ContainsKey("X-Requested-With")
                && context.Request.Headers.Accept.ToString().Contains("text/html")
                && !context.Response.HasStarted
                && !context.Items.ContainsKey(_registeredKey)
                && !_fileExtensionRegex.IsMatch(context.Request.Path.Value ?? string.Empty))
            {
                context.Items[_registeredKey] = true;
                context.Response.OnStarting(() =>
                {
                    if (context.Response.StatusCode == 200)
                        pageOptimizerService.AddLinkHeaders(context);

                    return Task.CompletedTask;
                });
            }

            await _next(context);
        }
    }
}
