using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Admirably.PageOptimizer.Rss.Models;

namespace Admirably.PageOptimizer.Rss.Services
{
    public class RssFeedService
    {
        private readonly IEnumerable<RssFeedRegistration> _registrations;
        private readonly IMemoryCache _memoryCache;
        private readonly RssFeedGenerator _generator;
        private readonly PageOptimizerConfig _config;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RssFeedService> _logger;

        // Pre-built per-feed semaphores to prevent cache stampede on each feed independently.
        private readonly Dictionary<string, SemaphoreSlim> _semaphores;

        public RssFeedService(
            IEnumerable<RssFeedRegistration> registrations,
            IMemoryCache memoryCache,
            RssFeedGenerator generator,
            PageOptimizerConfig config,
            IServiceProvider serviceProvider,
            ILogger<RssFeedService> logger)
        {
            _registrations = registrations;
            _memoryCache = memoryCache;
            _generator = generator;
            _config = config;
            _serviceProvider = serviceProvider;
            _logger = logger;

            var paths = registrations.Select(r => r.Path).ToList();
            var duplicates = paths.GroupBy(p => p, StringComparer.OrdinalIgnoreCase)
                                  .Where(g => g.Count() > 1)
                                  .Select(g => g.Key)
                                  .ToList();

            if (duplicates.Any())
                throw new InvalidOperationException(
                    $"Duplicate RSS feed paths detected: {string.Join(", ", duplicates)}. Each feed must have a unique path.");

            _semaphores = paths.ToDictionary(
                p => p,
                _ => new SemaphoreSlim(1, 1),
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task<string?> GetFeedXmlAsync(string path)
        {
            var registration = _registrations.FirstOrDefault(r =>
                string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase));

            if (registration == null)
                return null;

            var cacheKey = $"RssFeed_{path}";

            if (_memoryCache.TryGetValue(cacheKey, out string? cached))
            {
                _logger.LogDebug("RSS feed {Path} found in cache.", path);
                return cached;
            }

            var semaphore = _semaphores[path];
            await semaphore.WaitAsync();
            try
            {
                if (_memoryCache.TryGetValue(cacheKey, out cached))
                {
                    _logger.LogDebug("RSS feed {Path} was refreshed by another thread.", path);
                    return cached;
                }

                _logger.LogInformation("RSS feed {Path} cache is stale or empty. Refreshing.", path);
                return await RefreshFeedAsync(registration, cacheKey);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public bool HasFeed(string path) =>
            _semaphores.ContainsKey(path);

        private async Task<string> RefreshFeedAsync(RssFeedRegistration registration, string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(_config.BaseUrl))
                throw new InvalidOperationException($"RSS feed '{registration.Path}' requires WithBaseUrl() to be set — the channel <link> element is mandatory in RSS 2.0.");

            IEnumerable<RssItem> items;

            using (var scope = _serviceProvider.CreateScope())
            {
                items = await registration.Source(scope.ServiceProvider);
            }

            var xml = _generator.GenerateFeed(
                items,
                registration.Title,
                registration.Description,
                _config.BaseUrl);

            _memoryCache.Set(cacheKey, xml, registration.CacheDuration);

            _logger.LogInformation("RSS feed {Path} refreshed and cached for {Duration}.", registration.Path, registration.CacheDuration);

            return xml;
        }
    }
}
