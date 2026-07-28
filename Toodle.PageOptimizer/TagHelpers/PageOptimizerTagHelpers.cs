using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Admirably.PageOptimizer.Models;

namespace Admirably.PageOptimizer
{
    /// <summary>
    /// TagHelper for generating meta data tags for a web page.
    /// </summary>
    [HtmlTargetElement("page-optimizer")]
    public class PageOptimizerTagHelper : TagHelper
    {
        private readonly IPageOptimizerService _pageOptimizerService;
        private readonly JsonSerializerOptions _jsonOptions;

        public PageOptimizerTagHelper(IPageOptimizerService pageOptimizerService)
        {
            _pageOptimizerService = pageOptimizerService;
            // UnsafeRelaxedJsonEscaping keeps '+' and non-ASCII literal —
            // JavaScriptEncoder.Create(UnicodeRanges.All) force-escapes HTML-sensitive
            // ASCII (every '+' came out as a u002B escape) no matter which ranges are allowed. It also
            // leaves '<' unescaped, so GenerateBreadcrumbJsonLd re-escapes '<' to keep
            // a "</script>" in breadcrumb data from breaking out of the script block.
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Clear the output tag itself since we're just injecting meta tags
            output.TagName = null;

            var metaTags = new List<string>();

            // Add all meta tags
            AddRobotsMetaTag(metaTags);
            AddTitleTag(metaTags);
            AddDescriptionMetaTag(metaTags);
            AddCanonicalUrl(metaTags);
            AddSiteName(metaTags);
            AddLocale(metaTags);

            AddImageMetaTags(metaTags);

            var ogType = _pageOptimizerService.GetOgType();
            if (ogType != null)
            {
                metaTags.Add($"<meta property=\"og:type\" content=\"{ogType.TypeName}\" />");
                ogType.RenderTags(metaTags);
            }

            var twitterCard = _pageOptimizerService.GetTwitterCard();
            if (twitterCard.HasValue)
            {
                var cardValue = twitterCard.Value switch
                {
                    TwitterCard.SummaryLargeImage => "summary_large_image",
                    TwitterCard.App => "app",
                    TwitterCard.Player => "player",
                    _ => "summary"
                };
                metaTags.Add($"<meta name=\"twitter:card\" content=\"{cardValue}\" />");
            }

            // Add the meta tags
            if (metaTags.Any())
                output.Content.AppendHtml(string.Join("\n", metaTags) + "\n");

            // Add breadcrumbs JSON-LD if we have any
            var breadcrumbs = _pageOptimizerService.GetBreadCrumbs();
            if (breadcrumbs.Any())
            {
                var jsonLd = GenerateBreadcrumbJsonLd(breadcrumbs);
                output.Content.AppendHtml($"\n<script type=\"application/ld+json\">\n{jsonLd}\n</script>");
            }
        }

        private void AddImageMetaTags(List<string> metaTags)
        {
            var image = _pageOptimizerService.GetMetaImage();
            if (string.IsNullOrWhiteSpace(image))
                return;

            metaTags.Add($"<meta property=\"og:image\" content=\"{HtmlEncoder.Default.Encode(image)}\" />");

            var width = _pageOptimizerService.GetMetaImageWidth();
            if (width.HasValue)
                metaTags.Add($"<meta property=\"og:image:width\" content=\"{width.Value}\" />");

            var height = _pageOptimizerService.GetMetaImageHeight();
            if (height.HasValue)
                metaTags.Add($"<meta property=\"og:image:height\" content=\"{height.Value}\" />");

            var alt = _pageOptimizerService.GetMetaImageAlt();
            if (!string.IsNullOrWhiteSpace(alt))
            {
                metaTags.Add($"<meta property=\"og:image:alt\" content=\"{HtmlEncoder.Default.Encode(alt)}\" />");
                metaTags.Add($"<meta name=\"twitter:image:alt\" content=\"{HtmlEncoder.Default.Encode(alt)}\" />");
            }

            metaTags.Add($"<meta name=\"twitter:image\" content=\"{HtmlEncoder.Default.Encode(image)}\" />");
        }

        private void AddSiteName(List<string> metaTags)
        {
            var siteName = _pageOptimizerService.GetSiteName();
            if (!string.IsNullOrWhiteSpace(siteName))
            {
                metaTags.Add($"<meta property=\"og:site_name\" content=\"{HtmlEncoder.Default.Encode(siteName)}\" />");
            }
        }

        private void AddRobotsMetaTag(List<string> metaTags)
        {
            var robots = _pageOptimizerService.GetRobots();
            if (!string.IsNullOrWhiteSpace(robots))
                metaTags.Add($"<meta name=\"robots\" content=\"{HtmlEncoder.Default.Encode(robots)}\">");
        }

        private void AddCanonicalUrl(List<string> metaTags)
        {
            var canonicalUrl = _pageOptimizerService.GetCanonicalUrl();
            if (canonicalUrl == null)
                return;

            var url = canonicalUrl.AbsoluteUri;
            if (url.Length > canonicalUrl.GetLeftPart(UriPartial.Authority).Length + 1)
                url = url.TrimEnd('/');   // trim only when there's a real path

            var encoded = HtmlEncoder.Default.Encode(url);
            metaTags.Add($"<link rel=\"canonical\" href=\"{encoded}\" />");
            metaTags.Add($"<meta property=\"og:url\" content=\"{encoded}\" />");
        }

        private void AddTitleTag(List<string> metaTags)
        {
            var title = _pageOptimizerService.GetMetaTitle();
            if (string.IsNullOrWhiteSpace(title))
                return;

            var siteName = _pageOptimizerService.GetSiteName();
            var separator = _pageOptimizerService.GetTitleSeparator();

            var formattedTitle = string.IsNullOrWhiteSpace(siteName)
                ? HtmlEncoder.Default.Encode(title)
                : $"{HtmlEncoder.Default.Encode(title)} {HtmlEncoder.Default.Encode(separator)} {HtmlEncoder.Default.Encode(siteName)}";

            metaTags.Add($"<title>{formattedTitle}</title>");
            metaTags.Add($"<meta property=\"og:title\" content=\"{formattedTitle}\" />");
            metaTags.Add($"<meta name=\"twitter:title\" content=\"{HtmlEncoder.Default.Encode(title)}\" />");
        }

        private void AddDescriptionMetaTag(List<string> metaTags)
        {
            var description = _pageOptimizerService.GetMetaDescription();
            if (!string.IsNullOrWhiteSpace(description))
            {
                metaTags.Add($"<meta name=\"description\" content=\"{HtmlEncoder.Default.Encode(description)}\">");
                metaTags.Add($"<meta property=\"og:description\" content=\"{HtmlEncoder.Default.Encode(description)}\" />");
                metaTags.Add($"<meta name=\"twitter:description\" content=\"{HtmlEncoder.Default.Encode(description)}\" />");
            }
        }

        private void AddLocale(List<string> metaTags)
        {
            var locale = _pageOptimizerService.GetLocale();
            if (!string.IsNullOrWhiteSpace(locale))
            {
                metaTags.Add($"<meta property=\"og:locale\" content=\"{locale}\" />");
            }
        }

        private string GenerateBreadcrumbJsonLd(IReadOnlyList<(string Title, string Url)> breadcrumbs)
        {
            var validCrumbs = breadcrumbs
                .Where(b => !string.IsNullOrWhiteSpace(b.Title))
                .ToList();

            if (validCrumbs.Count == 0)
                return string.Empty;

            var baseUrl = _pageOptimizerService.GetBaseUrl()?.ToString();
            var itemListElement = new List<object>();

            for (int i = 0; i < validCrumbs.Count; i++)
            {
                var breadcrumb = validCrumbs[i];

                var item = new Dictionary<string, object>
                {
                    { "@type", "WebPage" },
                    { "name", breadcrumb.Title }
                };

                var isLastItem = i == validCrumbs.Count - 1;
                if (!isLastItem && !string.IsNullOrEmpty(breadcrumb.Url))
                {
                    if (Uri.TryCreate(breadcrumb.Url, UriKind.Absolute, out _))
                        item.Add("@id", breadcrumb.Url);
                    else if (!string.IsNullOrWhiteSpace(baseUrl))
                        item.Add("@id", PageOptimizerApp.ResolveAbsoluteUrl(breadcrumb.Url, baseUrl));
                }

                itemListElement.Add(new Dictionary<string, object>
                {
                    { "@type", "ListItem" },
                    { "position", i + 1 },
                    { "item", item }
                });
            }

            var jsonLdData = new Dictionary<string, object>
            {
                { "@context", "https://schema.org" },
                { "@type", "BreadcrumbList" },
                { "itemListElement", itemListElement }
            };

            return JsonSerializer.Serialize(jsonLdData, _jsonOptions)
                .Replace("<", "\\u003C");
        }
    }
}
