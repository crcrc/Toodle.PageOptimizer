using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sixpoints.PageOptimizer
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds metadata and breadcrumb services to the service collection
        /// </summary>
        public static IServiceCollection AddMetaData(
            this IServiceCollection services,
            string siteName,
            string baseUrl,
            string locale = "en-US")
        {
            services.AddScoped<IMetaDataService>(sp => new MetaDataService(siteName, baseUrl, locale));
            return services;
        }
    }
}
