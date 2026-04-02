using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Toodle.PageOptimizer.Sitemap.Models;

namespace Toodle.PageOptimizer.Sitemap.Services
{
    

    public class SitemapService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly SitemapGenerator _sitemapGenerator;
        private readonly PageOptimizerConfig _config;
        private readonly ILogger<SitemapService> _logger;

        private const string SitemapCacheKey = "SitemapXml";

        // This semaphore ensures that only one thread can refresh the sitemap at a time.
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public SitemapService(
            IServiceProvider serviceProvider,
            IMemoryCache memoryCache,
            SitemapGenerator sitemapGenerator,
            PageOptimizerConfig config,
            ILogger<SitemapService> logger)
        {
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
            _sitemapGenerator = sitemapGenerator;
            _config = config;
            _logger = logger;
        }

        public async Task<string?> GetSitemapXmlAsync()
        {
            // 1. Try to get the sitemap from the cache first.
            if (_memoryCache.TryGetValue(SitemapCacheKey, out string? cachedSitemap))
            {
                _logger.LogDebug("Sitemap found in cache. Serving cached version.");
                return cachedSitemap;
            }

            // 2. If not in cache, acquire the lock to prevent multiple refreshes.
            await _semaphore.WaitAsync();
            try
            {
                // 3. Double-check if another thread has already refreshed the cache while we were waiting for the lock.
                if (_memoryCache.TryGetValue(SitemapCacheKey, out cachedSitemap))
                {
                    _logger.LogDebug("Sitemap was refreshed by another thread. Serving new cached version.");
                    return cachedSitemap;
                }

                // 4. If we are the ones to refresh, do the work.
                _logger.LogInformation("Sitemap cache is stale or empty. Starting refresh.");
                return await RefreshSitemapAsync();
            }
            finally
            {
                // 5. ALWAYS release the semaphore.
                _semaphore.Release();
            }
        }

        private async Task<string> RefreshSitemapAsync()
        {
            var allUrls = new List<SitemapUrl>();

            // Create a DI scope to resolve scoped services like DbContext.
            using (var scope = _serviceProvider.CreateScope())
            {
                var sources = scope.ServiceProvider.GetServices<ISitemapSource>();

                foreach (var source in sources)
                {
                    var sourceUrls = await source.GetUrlsAsync();
                    allUrls.AddRange(sourceUrls);
                }
            }

            string sitemapXml = _sitemapGenerator.GenerateSitemap(allUrls.DistinctBy(u => u.Location));

            var cacheDuration = _config.SitemapOptions?.CacheDuration ?? TimeSpan.FromHours(4);
            _memoryCache.Set(SitemapCacheKey, sitemapXml, cacheDuration);

            _logger.LogInformation("Sitemap refresh completed. Cached {UrlCount} unique URLs for {Expiration}.",
                allUrls.DistinctBy(u => u.Location).Count(), cacheDuration);

            return sitemapXml;
        }
    }
}
