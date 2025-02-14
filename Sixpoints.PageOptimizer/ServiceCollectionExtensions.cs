using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sixpoints.PageOptimizer
{
    /// <summary>
    /// Extension methods for adding metadata and breadcrumb services to the service collection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds metadata and breadcrumb services to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <param name="siteName">The name of the website.</param>
        /// <param name="baseUrl">The base URL of the website (e.g., https://example.com).</param>
        /// <param name="locale">The default locale for the website (e.g., en-US). Default is "en-US".</param>
        /// <param name="homeText">The text that will appear for the first BreadcrumbLink to the site root (e.g. Home)</param>
        /// <returns>The updated service collection.</returns>
        /// <exception cref="ArgumentException">Thrown when siteName or baseUrl is null or empty, or if baseUrl is invalid.</exception>
        public static IServiceCollection AddMetaData(
            this IServiceCollection services,
            string siteName,
            string baseUrl,
            string locale = "en-US",
            string homeText = "Home")
        {
            services.AddScoped<IMetaDataService>(sp => new MetaDataService(siteName, baseUrl, locale, homeText));
            return services;
        }
    }
}
