using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toodle.PageOptimizer.Sitemap.Models;
using Toodle.PageOptimizer.Sitemap.Services;

namespace Toodle.PageOptimizer.Sitemap
{
    public static class SitemapServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all necessary services for sitemap generation.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="configureOptions">An action to configure sitemap options, like cache expiration.</param>
        public static IServiceCollection AddSitemap(
            this IServiceCollection services,
            Action<SitemapOptions>? configureOptions = null)
        {
            // Register the options, allowing the user to configure them.
            services.Configure(configureOptions ?? (opts => { }));

            services.AddMemoryCache();
            services.AddSingleton<SitemapGenerator>();

            // The orchestrator service MUST be a singleton to manage the lock correctly.
            services.AddSingleton<SitemapService>();

            return services;
        }

        /// <summary>
        /// Registers a function that will be used as a sitemap source.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="urlProviderFunc">The function that returns a collection of sitemap URLs.</param>
        public static IServiceCollection AddSitemapSource(
            this IServiceCollection services,
            Func<IServiceProvider, Task<IEnumerable<SitemapUrl>>> urlProviderFunc)
        {
            // This line was causing the error because the class below was missing.
            services.AddScoped<ISitemapSource>(sp => new FuncSitemapSource(urlProviderFunc, sp));
            return services;
        }

        /// <summary>
        /// Registers a class that implements ISitemapSource.
        /// </summary>
        public static IServiceCollection AddSitemapSource<T>(this IServiceCollection services)
            where T : class, ISitemapSource
        {
            return services.AddScoped<ISitemapSource, T>();
        }

        // =======================================================================
        // == THIS IS THE MISSING PIECE THAT FIXES THE ERROR ==
        // =======================================================================
        /// <summary>
        /// An internal adapter class that wraps a user-provided function into an ISitemapSource.
        /// This allows for simple, inline registration of sitemap sources.
        /// </summary>
        private class FuncSitemapSource : ISitemapSource
        {
            private readonly Func<IServiceProvider, Task<IEnumerable<SitemapUrl>>> _urlProviderFunc;
            private readonly IServiceProvider _serviceProvider;

            /// <summary>
            /// This is the constructor that was missing. It accepts the user's function
            /// and the service provider.
            /// </summary>
            public FuncSitemapSource(
                Func<IServiceProvider, Task<IEnumerable<SitemapUrl>>> urlProviderFunc,
                IServiceProvider serviceProvider)
            {
                _urlProviderFunc = urlProviderFunc ?? throw new ArgumentNullException(nameof(urlProviderFunc));
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }

            /// <summary>
            /// Executes the user's function to get the sitemap URLs.
            /// </summary>
            public Task<IEnumerable<SitemapUrl>> GetUrlsAsync()
            {
                // When this is called by the SitemapService, it will invoke the
                // lambda expression you defined in Program.cs.
                return _urlProviderFunc(_serviceProvider);
            }
        }
    }
}
