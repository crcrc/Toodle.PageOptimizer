using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

using Toodle.PageOptimizer.Models;

namespace Toodle.PageOptimizer
{
    public interface IPageOptimizerService
    {
        public string GetMetaTitle();
        public string GetMetaDescription();
        public bool IsNoIndex();
        public string GetSiteName();
        public string GetTitleSeparator();
        public string GetLocale();
        public Uri? GetCanonicalUrl();
        public Uri GetBaseUrl();
        public IReadOnlyList<(string Title, string Url)> GetBreadCrumbs();


        public IPageOptimizerService AddPreconnect(string domain, bool crossOrigin = true);
        public IPageOptimizerService AddPreload(string url, AssetType AssetType, bool crossOrigin = false);
        public IPageOptimizerService SetMetaTitle(string title);
        public IPageOptimizerService SetLocale(string locale);
        public IPageOptimizerService SetMetaDescription(string description);
        public IPageOptimizerService SetNoIndex();
        public IPageOptimizerService SetCanonicalUrl(string relativePath);
        public IPageOptimizerService AddBreadCrumb(string title, string url);
        public IPageOptimizerService ClearBreadcrumbs();
        public void AddLinkHeaders(HttpContext context);
    }

    public class PageOptimizerService : IPageOptimizerService
    {
        private readonly PageOptimizerConfig _config;

        private readonly string _siteName;
        private readonly string _titleSeparator;
        private readonly Uri _baseUrl;
        private readonly List<(string Domain, bool CrossOrigin)> _preconnectDomains = new List<(string Domain, bool CrossOrigin)>();
        private readonly List<(string Url, AssetType AssetType, bool CrossOrigin)> _preloadResources = new List<(string Url, AssetType AssetType, bool CrossOrigin)>();
        private string _metaTitle = string.Empty;
        private Uri? _canonicalUrl;
        private string _metaDescription = string.Empty;
        private bool _noIndex = false;
        private string _locale;
        private readonly List<(string Title, string Url)> _breadcrumbs = new List<(string Title, string Url)>();

        public string GetMetaTitle() => _metaTitle;
        public string GetMetaDescription() => _metaDescription;
        public bool IsNoIndex() => _noIndex;
        public string GetSiteName() => _siteName;
        public string GetTitleSeparator() => _titleSeparator;
        public string GetLocale() => _locale;
        public Uri? GetCanonicalUrl() => _canonicalUrl;
        public Uri GetBaseUrl() => _baseUrl;
        public IReadOnlyList<(string Title, string Url)> GetBreadCrumbs() => _breadcrumbs.AsReadOnly();

        public PageOptimizerService(PageOptimizerConfig config)
        {
            _config = config;

            if (!string.IsNullOrWhiteSpace(_config.SiteName))
                _siteName = _config.SiteName;

            if (!string.IsNullOrWhiteSpace(_config.TitleSeparator))
                _titleSeparator = _config.TitleSeparator;

            if (!string.IsNullOrWhiteSpace(_config.BaseUrl))
                _baseUrl = new Uri(_config.BaseUrl);

            if (_config.PreconnectDomains != null)
                _preconnectDomains.AddRange(_config.PreconnectDomains);

            if (_config.PreloadResources != null)
                _preloadResources.AddRange(_config.PreloadResources);

            if (_config.DefaultBreadcrumbs != null)
                _breadcrumbs.AddRange(_config.DefaultBreadcrumbs);
        }

        public IPageOptimizerService AddPreconnect(string domain, bool crossOrigin = true)
        {
            _preconnectDomains.Add((domain, crossOrigin));
            return this;
        }

        public IPageOptimizerService AddPreload(string url, AssetType AssetType, bool crossOrigin = false)
        {
            _preloadResources.Add((url, AssetType, crossOrigin));
            return this;
        }

        public void AddLinkHeaders(HttpContext context)
        {
            if (_preconnectDomains.Count == 0 && _preloadResources.Count == 0)
                return;

            var linkValues = new List<string>();

            // Add preconnect links
            foreach (var (domain, crossOrigin) in _preconnectDomains)
            {
                linkValues.Add(crossOrigin
                    ? $"<{domain}>; rel=preconnect; crossorigin"
                    : $"<{domain}>; rel=preconnect");
            }

            // Add preload links
            foreach (var (url, type, crossOrigin) in _preloadResources)
            {
                var asValue = type.ToString().ToLowerInvariant();
                linkValues.Add(crossOrigin
                    ? $"<{url}>; rel=preload; as={asValue}; crossorigin"
                    : $"<{url}>; rel=preload; as={asValue}");
            }

            if (linkValues.Count > 0)
                context.Response.Headers.AppendCommaSeparatedValues("Link", linkValues.ToArray());
        }

        public IPageOptimizerService SetMetaTitle(string title)
        {
            _metaTitle = title;
            return this;
        }

        public IPageOptimizerService SetLocale(string locale)
        {
            _locale = locale;
            return this;
        }

        public IPageOptimizerService SetMetaDescription(string description)
        {
            _metaDescription = description;
            return this;
        }

        public IPageOptimizerService SetNoIndex()
        {
            _noIndex = true;
            return this;
        }

        public IPageOptimizerService SetCanonicalUrl(string relativePath)
        {
            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            if (_baseUrl == null)
                throw new InvalidOperationException("Base URL must be set before setting canonical URL");

            try
            {
                _canonicalUrl = new Uri(_baseUrl, relativePath);
            }
            catch (UriFormatException ex)
            {
                throw new ArgumentException($"Invalid relative path format: {relativePath}", nameof(relativePath), ex);
            }
            return this;
        }

        public IPageOptimizerService AddBreadCrumb(string title, string url = "")
        {
            _breadcrumbs.Add((title, url));
            return this;
        }

        public IPageOptimizerService ClearBreadcrumbs()
        {
            _breadcrumbs.Clear();
            return this;
        }


    }


}

