# Toodle.PageOptimizer
![NuGet Version](https://img.shields.io/nuget/v/Toodle.PageOptimizer)
![NuGet Downloads](https://img.shields.io/nuget/dt/Toodle.PageOptimizer)

A high-performance metadata, breadcrumb, and resource optimization manager for ASP.NET Core. It streamlines SEO best practices and improves Core Web Vitals (LCP/FCP) by automating resource hinting and header management.

See [https://www.pricewatchdog.co.uk](https://www.pricewatchdog.co.uk) and [https://www.competitions-whale.co.uk](https://www.competitions-whale.co.uk) as examples of websites using this library.
## Features
- Fluent SEO API: Easily manage Meta Titles, Descriptions, Canonical URLs, og:type (with typed article/product/profile objects), and Twitter Card types.
- Social Sharing: Full Open Graph and Twitter Card support including per-page and global default images.
- HTTP Link Headers: Injects rel=preload and rel=preconnect headers into the HTTP response for faster asset discovery.
- Static File Caching: Granular control over Cache-Control headers for specific file extensions or paths.
- HTTPS Compression: One-click setup for Brotli and Gzip.
- Structured Data: Automatic generation of JSON-LD BreadcrumbList for Google Search results.
- Flexible Sitemaps: Register dynamic sitemap sources from databases or static lists.
- Localization: Integrated support for RequestCulture and og:locale tags.

## Installation
```Bash
dotnet add package Toodle.PageOptimizer
```

### Service Configuration
Register the service in Program.cs. This is where you define your compression and sitemap logic.

```c#
builder.Services.AddPageOptimizer(options =>
{
    options.EnableHttpsCompression = true; // Enables Brotli/Gzip for HTTPS
    options.UseRequestCulture = new RequestCulture("en-GB");
})
.AddSitemapSource(async (serviceProvider) =>
{
    // Example: Fetching dynamic product links for the sitemap
    using var scope = serviceProvider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var products = await db.Products.ToListAsync();

    return products.Select(p => new SitemapUrl {
        Location = $"/products/{p.Slug}",
        Priority = 0.8m,
        ChangeFrequency = ChangeFrequency.Weekly
    });
});
```

### Middleware & App Configuration
Configure your global site defaults. This locks the global configuration to prevent accidental runtime changes.

```C#
app.ConfigurePageOptimizer()
    .WithBaseTitle("Price Watchdog", "|")
    .WithBaseUrl("https://www.pricewatchdog.co.uk")
    .WithDefaultImage("/images/default-share.jpg") // resolved to absolute using base URL
    .AddDefaultPreconnect("https://res.cloudinary.com")
    // Adds Link: </js/bundle.js>; rel=preload; as=script to every GET response header
    .AddDefaultPreload("/js/bundle.min.js", AssetType.Script) 
    .AddDefaultBreadcrumb("Home", "/")
    .AddStaticFileCacheHeaders(opt =>
    {
        opt.IsPublic = true;
        opt.MaxAge = TimeSpan.FromDays(7);
        opt.FileExtensions = new[] { ".js", ".css", ".ico", ".webp" };
    })
    .ServeSitemap(opt =>
    {
        opt.Path = "/sitemap.xml";
        opt.CacheDuration = TimeSpan.FromHours(4);
    });

app.UsePageOptimizer(); // Enables header injection and SEO middleware
```

### Rendering Meta Tags & Breadcrumbs
Add the following to your _ViewImports.cshtml:

```
@addTagHelper *, Toodle.PageOptimizer
```

In Layout (_Layout.cshtml)

The <page-optimizer /> tag helper handles the title, meta, link rel="canonical", and the JSON-LD Breadcrumb script.

```html
<head>
    <meta charset="utf-8" />
    <page-optimizer />
</head>
```

### UI Breadcrumbs (Partial View)

Create a partial view (e.g., _Breadcrumbs.cshtml) to render the visual navigation using the injected service.

```Razor CSHTML
@inject Toodle.PageOptimizer.IPageOptimizerService pageOptimizerService
@{
    var breadcrumbs = pageOptimizerService.GetBreadCrumbs();
}

@if (breadcrumbs.Any())
{
    <nav aria-label="breadcrumb">
        <ol class="breadcrumb">
            @foreach (var crumb in breadcrumbs)
            {
                var isLast = breadcrumbs.Last() == crumb;
                <li class="breadcrumb-item @(isLast ? "active" : "")">
                    @if (isLast)
                    {
                        <span aria-current="page">@crumb.Title</span>
                    }
                    else
                    {
                        <a href="@crumb.Url">@crumb.Title</a>
                    }
                </li>
            }
        </ol>
    </nav>
}
```

### Controller Usage
Update page metadata and breadcrumbs dynamically within your actions.

```C#

public class ProductController : Controller
{
    private readonly IPageOptimizerService _optimizer;

    public ProductController(IPageOptimizerService optimizer)
    {
        _optimizer = optimizer;
    }

    public IActionResult Details(string slug)
    {
        var product = _db.Products.Find(slug);

        _optimizer
            .SetMetaTitle(product.Name)
            .SetMetaDescription(product.Summary)
            .SetCanonicalUrl($"/products/{product.Slug}") // relative or absolute
            .SetMetaImage(product.ImageUrl, width: 1200, height: 630, alt: product.Name)
            .SetOgType(new OgTypeProduct
            {
                PriceAmount = product.Price,
                PriceCurrency = "GBP",
                Availability = "instock"
            })
            .SetTwitterCard(TwitterCard.SummaryLargeImage)
            .AddBreadCrumb("Products", "/products")
            .AddBreadCrumb(product.Name); // Current page (no URL)

        // Prevent indexing for specific conditions
        if (product.IsDiscontinued) _optimizer.SetNoIndex(); // shortcut for SetRobots("noindex")

        // Or use the full robots string for more control
        // _optimizer.SetRobots("noindex, nofollow");

        return View(product);
    }
}
```

## Advanced Mechanics
### Resource Hinting (Link Headers)
The library automatically appends Link headers to the HTTP response for all preconnect and preload resources defined in configuration. This triggers "Early Hints" in supported browsers and CDNs, allowing assets to begin downloading while the server is still processing the HTML.

### Response Compression
When EnableHttpsCompression is set to true, the library automatically configures:

- Brotli & Gzip at Optimal compression levels.
- Support for image/svg+xml and other standard MIME types.

### Social Sharing (og:image / twitter:image)
Set a global default share image in `ConfigurePageOptimizer()` using `WithDefaultImage()`. Individual pages can override it with `SetMetaImage()`. Both methods accept relative paths (resolved against the base URL) or absolute URLs, making it easy to serve images from a CDN on a different domain.

### Robots Meta Tag
`SetNoIndex()` is a shortcut for the common case. `SetRobots()` accepts any valid robots directives string for full control:

```csharp
_optimizer.SetNoIndex();                          // <meta name="robots" content="noindex">
_optimizer.SetRobots("noindex, nofollow");        // <meta name="robots" content="noindex, nofollow">
_optimizer.SetRobots("noarchive");               // <meta name="robots" content="noarchive">
```

The tag is not rendered unless one of these methods is called.

### og:type
`SetOgType()` accepts a typed object that sets the `og:type` tag and automatically renders the matching type-specific properties:

| Type | og:type | Extra tags rendered |
|---|---|---|
| `OgTypeWebsite` | `website` | none |
| `OgTypeArticle` | `article` | `article:published_time`, `article:modified_time`, `article:author`, `article:section`, `article:tag` |
| `OgTypeProduct` | `product` | `product:price:amount`, `product:price:currency`, `product:availability` |
| `OgTypeProfile` | `profile` | `profile:first_name`, `profile:last_name`, `profile:username` |

All properties on each type are optional. Not rendered unless `SetOgType()` is called. Custom types can be created by extending the `OgType` base class and overriding `RenderTags()`.

### Twitter Card
`SetTwitterCard()` accepts a `TwitterCard` enum value (`Summary`, `SummaryLargeImage`, `App`, `Player`). Not rendered unless explicitly set.

### Static File Cache Middleware
The StaticFileCacheHeaderMiddleware intercepts requests for static assets and applies Cache-Control headers based on your FileExtensions and Paths configuration, ensuring high cache hit ratios. Headers are applied just before the response is flushed, so they take precedence over any headers set by UseStaticFiles.