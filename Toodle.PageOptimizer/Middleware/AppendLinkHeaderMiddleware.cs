using Microsoft.AspNetCore.Http;
using System;
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

        // Pipeline re-execution (UseExceptionHandler / UseStatusCodePagesWithReExecute)
        // runs this middleware twice on the same HttpContext; without this guard both
        // passes register OnStarting and every link-value is emitted twice.
        private const string _registeredKey = "Admirably.PageOptimizer.LinkHeaderRegistered";

        public async Task InvokeAsync(HttpContext context, IPageOptimizerService pageOptimizerService)
        {
            // Only the method can be decided up front. Whether the response is HTML is decided
            // inside the OnStarting callback: gating on the request's Accept header dropped the
            // header for clients that do not advertise text/html (HEAD probes, fetch(), htmx,
            // CDNs), while a request-path extension test could not see that a response at an
            // extensionless route was in fact RSS or JSON. Response.ContentType is populated by
            // the time the callback runs and answers the question directly.
            if ((context.Request.Method == "GET" || context.Request.Method == "HEAD")
                && !context.Request.Headers.ContainsKey("X-Requested-With")
                && !context.Response.HasStarted
                && !context.Items.ContainsKey(_registeredKey))
            {
                context.Items[_registeredKey] = true;
                context.Response.OnStarting(() =>
                {
                    if (context.Response.StatusCode == StatusCodes.Status200OK
                        && context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
                        pageOptimizerService.AddLinkHeaders(context);

                    return Task.CompletedTask;
                });
            }

            await _next(context);
        }
    }
}
