# Toodle.PageOptimizer
![NuGet Version](https://img.shields.io/nuget/v/Toodle.PageOptimizer)
![NuGet Downloads](https://img.shields.io/nuget/dt/Toodle.PageOptimizer)

A high-performance metadata, breadcrumb, and resource optimization manager for ASP.NET Core. It streamlines SEO best practices and improves Core Web Vitals (LCP/FCP) by automating resource hinting and header management.

See [https://www.pricewatchdog.co.uk](https://www.pricewatchdog.co.uk) and [https://www.competitions-whale.co.uk](https://www.competitions-whale.co.uk) as examples of websites using this library.
## Features
- Fluent SEO API: Easily manage Meta Titles, Descriptions, and Canonical URLs.
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
            .SetCanonicalUrl($"/products/{product.Slug}")
            .AddBreadCrumb("Products", "/products")
            .AddBreadCrumb(product.Name); // Current page (no URL)

        // Prevent indexing for specific conditions
        if (product.IsDiscontinued) _optimizer.SetNoIndex();

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

### Static File Cache Middleware
The StaticFileCacheHeaderMiddleware intercepts requests for static assets and applies Cache-Control headers based on your FileExtensions and Paths configuration, ensuring high cache hit ratios.