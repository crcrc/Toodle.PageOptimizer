using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sixpoints.PageOptimizer
{
    [HtmlTargetElement("meta-data-tags")]
    public class MetaDataTagHelper : TagHelper
    {
        private readonly IMetaDataService _metaService;
        private readonly JsonSerializerOptions _jsonOptions;

        public MetaDataTagHelper(IMetaDataService metaService)
        {
            _metaService = metaService;
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
            AddPreloadImageTags(metaTags);
            AddCanonicalUrl(metaTags);
            AddSiteName(metaTags);
            AddLocale(metaTags);

            metaTags.Add($"<meta property=\"og:type\" content=\"website\" />");
            metaTags.Add($"<meta name=\"twitter:card\" content=\"summary\" />");

            // Add the meta tags
            if (metaTags.Any())
                output.Content.AppendHtml(string.Join("\n", metaTags) + "\n");


            // Add breadcrumbs JSON-LD if we have any
            var breadcrumbs = _metaService.GetBreadCrumbs();
            if (breadcrumbs.Any())
            {
                var jsonLd = GenerateBreadcrumbJsonLd(breadcrumbs);
                output.Content.AppendHtml($"\n<script type=\"application/ld+json\">\n{jsonLd}\n</script>");
            }
        }

        private void AddSiteName(List<string> metaTags)
        {
            var siteName = _metaService.GetSiteName();
            if (!string.IsNullOrWhiteSpace(siteName))
            {
                metaTags.Add($"<meta property=\"og:site_name\" content=\"{HtmlEncoder.Default.Encode(siteName)}\" />");
            }
        }

        private void AddRobotsMetaTag(List<string> metaTags)
        {
            if (_metaService.IsNoIndex())
            {
                metaTags.Add("<meta name=\"robots\" content=\"noindex\">");
            }
        }

        private void AddCanonicalUrl(List<string> metaTags)
        {
            var canonicalUrl = _metaService.GetCanonicalUrl();
            if (canonicalUrl is not null)
            {
                metaTags.Add($"<link rel=\"canonical\" href=\"{HtmlEncoder.Default.Encode(canonicalUrl.ToString().TrimEnd('/'))}\" />");
                metaTags.Add($"<meta property=\"og:url\" content=\"{canonicalUrl.ToString().TrimEnd('/')}\" />");
            }
        }

        private void AddTitleTag(List<string> metaTags)
        {
            var title = _metaService.GetMetaTitle();
            if (!string.IsNullOrWhiteSpace(title))
            {
                var siteName = _metaService.GetSiteName();
                var formattedTitle = $"{HtmlEncoder.Default.Encode(title)} | {HtmlEncoder.Default.Encode(siteName)}";
                metaTags.Add($"<title>{formattedTitle}</title>");
                metaTags.Add($"<meta property=\"og:title\" content=\"{formattedTitle}\" />");
                metaTags.Add($"<meta name=\"twitter:title\" content=\"{HtmlEncoder.Default.Encode(title)}\" />");
            }
        }

        private void AddDescriptionMetaTag(List<string> metaTags)
        {
            var description = _metaService.GetMetaDescription();
            if (!string.IsNullOrWhiteSpace(description))
            {
                metaTags.Add($"<meta name=\"description\" content=\"{HtmlEncoder.Default.Encode(description)}\">");
                metaTags.Add($"<meta property=\"og:description\" content=\"{HtmlEncoder.Default.Encode(description)}\" />");
                metaTags.Add($"<meta name=\"twitter:description\" content=\"{HtmlEncoder.Default.Encode(description)}\" />");
            }
        }

        private void AddLocale(List<string> metaTags)
        {
            var locale = _metaService.GetLocale();
            if (!string.IsNullOrWhiteSpace(locale))
            {
                metaTags.Add($"<meta property=\"og:locale\" content=\"{locale}\" />");
            }
        }

        private void AddPreloadImageTags(List<string> metaTags)
        {
            foreach (var url in _metaService.GetPreloadImageUrls())
            {
                metaTags.Add($"<link rel=\"preload\" as=\"image\" href=\"{HtmlEncoder.Default.Encode(url)}\">");
            }
        }

        private string GenerateBreadcrumbJsonLd(IReadOnlyList<BreadcrumbLink> breadcrumbs)
        {
            var itemListElement = new List<object>();

            for (int i = 0; i < breadcrumbs.Count; i++)
            {
                var breadcrumb = breadcrumbs[i];
                var item = new Dictionary<string, object>
                {
                    { "@type", "WebPage" },
                    { "name", breadcrumb.Title }
                };

                var isLastItem = i == breadcrumbs.Count - 1;
                if (!isLastItem && !string.IsNullOrEmpty(breadcrumb.Link))
                {
                    item.Add("@id", breadcrumb.Link);
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

            return JsonSerializer.Serialize(jsonLdData, _jsonOptions);
        }
    }
}
