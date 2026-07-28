# Admirably.PageOptimizer
![NuGet Version](https://img.shields.io/nuget/v/Admirably.PageOptimizer)
![NuGet Downloads](https://img.shields.io/nuget/dt/Admirably.PageOptimizer)

A high-performance metadata, breadcrumb, and resource optimization manager for ASP.NET Core. It streamlines SEO best practices and improves Core Web Vitals (LCP/FCP) by automating resource hinting and header management.

See [https://www.pricewatchdog.co.uk](https://www.pricewatchdog.co.uk) and [https://www.competitions-whale.co.uk](https://www.competitions-whale.co.uk) as examples of websites using this library.

## Features
- Fluent SEO API: Easily manage Meta Titles, Descriptions, Canonical URLs, og:type (with typed article/product/profile objects), and Twitter Card types.
- Social Sharing: Full Open Graph and Twitter Card support including per-page and global default images.
- HTTP Link Headers: Injects rel=preload and rel=preconnect headers into the HTTP response for faster asset discovery.
- Static File Caching: Granular control over Cache-Control headers for specific file extensions or paths, applied to 200, 304, and 206 responses.
- HTTPS Compression: One-click setup for Brotli and Gzip.
- Structured Data: Automatic generation of JSON-LD BreadcrumbList for Google Search results, safely encoded against script injection.
- Flexible Sitemaps: Register dynamic sitemap sources from databases or static lists. Relative URLs are resolved against your base URL automatically.
- RSS Feeds: Serve one or more cached RSS 2.0 feeds from any data source.
- robots.txt: Serve a generated robots.txt with optional custom rules. Sitemap URL included automatically.
- Localization: Integrated support for RequestCulture and og:locale tags.
- Startup safety: Configuration is validated and locked at startup. Paths are normalized (a missing leading `/` is added for you), and misconfiguration fails fast with a clear message instead of at request time.

## Installation
```bash
dotnet add package Admirably.PageOptimizer
```

## Service Configuration
Register the services in `Program.cs`. This is where you define compression, localization, sitemap sources, and RSS feeds.

```csharp
builder.Services.AddPageOptimizer(options =>
{
    options.EnableHttpsCompression = true; // Enables Brotli/Gzip for HTTPS
    options.UseRequestCulture = new RequestCulture("en-GB");
})
.AddSitemapSource(async (serviceProvider) =>
{
    // Example: fetching dynamic product links for the sitemap
    using var scope = serviceProvider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var products = await db.Products.ToListAsync();

    return products.Select(p => new SitemapUrl {
        Location = $"/products/{p.Slug}", // relative paths are resolved against your base URL
        Priority = 0.8m,
        ChangeFrequency = ChangeFrequency.Weekly
    });
})
.AddRssFeed(
    path: "/feed.xml",
    title: "Price Watchdog — Latest Deals",
    description: "The latest price drops and deals.",
    source: async (serviceProvider) =>
    {
        var db = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var posts = await db.Posts.OrderByDescending(p => p.PublishedAt).Take(20).ToListAsync();

        return posts.Select(p => new RssItem
        {
            Title = p.Title,
            Link = $"https://www.pricewatchdog.co.uk/blog/{p.Slug}", // absolute URL
            Description = p.Summary,
            PublishedDate = p.PublishedAt
        });
    },
    cacheDuration: TimeSpan.FromHours(1)); // optional, defaults to 2 hours
```

You can also register a class-based sitemap source:

```csharp
builder.Services.AddSitemapSource<MySitemapSource>(); // MySitemapSource : ISitemapSource
```

Multiple sitemap sources and multiple RSS feeds (at distinct paths) can be registered; sitemap sources are merged and de-duplicated by URL.

## Middleware & App Configuration
Configure your global site defaults after `app.Build()`. Calling `UsePageOptimizer()` locks the configuration to prevent accidental runtime changes.

```csharp
app.ConfigurePageOptimizer()
    .WithBaseTitle("Price Watchdog", "|")
    .WithBaseUrl("https://www.pricewatchdog.co.uk")
    .WithDefaultImage("/images/default-share.jpg") // resolved to absolute using base URL
    .AddDefaultPreconnect("https://res.cloudinary.com")
    // Adds Link: </js/bundle.min.js>; rel=preload; as=script to every HTML response header
    .AddDefaultPreload("/js/bundle.min.js", AssetType.Script)
    .AddDefaultBreadcrumb("Home", "/")
    .AddStaticFileCacheHeaders(opt =>
    {
        opt.IsPublic = true;
        opt.MaxAge = TimeSpan.FromDays(7);
        opt.FileExtensions = new[] { ".js", ".css", ".ico", ".webp" };
        // opt.Paths = new[] { "/js", "/css" }; // optional; segment-aware ("/js" won't match "/jsx")
    })
    .ServeSitemap(opt =>
    {
        opt.Path = "/sitemap.xml";
        opt.CacheDuration = TimeSpan.FromHours(4);
    })
    .ServeRobotsTxt(opt =>
    {
        opt.AdditionalRules = new[]
        {
            "Disallow: /admin/",
            "",
            "# Block AI scrapers",
            "User-agent: GPTBot",
            "Disallow: /"
        };
    });

app.UsePageOptimizer(); // Enables header injection and SEO middleware

app.UseStaticFiles(); // IMPORTANT: call AFTER UsePageOptimizer() — see note below
```

> **Middleware ordering:** `UsePageOptimizer()` must be called **before** `UseStaticFiles()`. Static file cache headers are applied via a response callback, so the PageOptimizer middleware needs to be earlier in the pipeline for static file requests to pass through it. This ordering is also what allows its `Cache-Control` value to take precedence over any header set by `UseStaticFiles`.

Notes:
- `WithBaseUrl()` is required before `ServeSitemap()`, `ServeRobotsTxt()`, and any RSS feed, and before using relative paths in `WithDefaultImage()`, `SetMetaImage()`, or `SetCanonicalUrl()`.
- `ServeSitemap()` validates at startup that at least one sitemap source is registered.
- All configured paths (sitemap, robots.txt, RSS feeds, static cache paths) are normalized — a missing leading `/` is added automatically. Duplicate RSS feed paths throw at startup.

## Rendering Meta Tags & Breadcrumbs
Add the following to your `_ViewImports.cshtml`:

```
@addTagHelper *, Admirably.PageOptimizer
```

### In your layout (`_Layout.cshtml`)
The `<page-optimizer />` tag helper renders the title, meta tags, `link rel="canonical"`, and the JSON-LD BreadcrumbList script.

```html
<head>
    <meta charset="utf-8" />
    <page-optimizer />
</head>
```

### UI Breadcrumbs (Partial View)
Create a partial view (e.g. `_Breadcrumbs.cshtml`) to render the visual navigation using the injected service.

```cshtml
@inject Admirably.PageOptimizer.IPageOptimizerService pageOptimizerService
@{
    var breadcrumbs = pageOptimizerService.GetBreadCrumbs();
}

@if (breadcrumbs.Any())
{
    <nav aria-label="breadcrumb">
        <ol class="breadcrumb">
            @for (var i = 0; i < breadcrumbs.Count; i++)
            {
                var crumb = breadcrumbs[i];
                var isLast = i == breadcrumbs.Count - 1;
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

## Controller Usage
Update page metadata and breadcrumbs dynamically within your actions.

```csharp
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
The library appends `Link` headers to successful (200) HTML responses for all preconnect and preload resources defined globally or per request (`AddPreconnect()` / `AddPreload()` on the service). This triggers "Early Hints" in supported browsers and CDNs, allowing assets to begin downloading while the server is still processing the HTML. Requests for static file extensions, non-GET requests, and AJAX requests (with `X-Requested-With`) are skipped.

### Response Compression
When `EnableHttpsCompression` is set to `true`, the library automatically configures:
- Brotli & Gzip at `Optimal` compression levels.
- Support for `image/svg+xml` in addition to the standard MIME types.

### Social Sharing (og:image / twitter:image)
Set a global default share image in `ConfigurePageOptimizer()` using `WithDefaultImage()`. Individual pages can override it with `SetMetaImage()`. Both methods accept relative paths (resolved against the base URL) or absolute URLs, making it easy to serve images from a CDN on a different domain. Optional width, height, and alt parameters render `og:image:width`, `og:image:height`, `og:image:alt`, and `twitter:image:alt`.

### Robots Meta Tag
`SetNoIndex()` is a shortcut for the common case. `SetRobots()` accepts any valid robots directives string for full control:

```csharp
_optimizer.SetNoIndex();                    // <meta name="robots" content="noindex">
_optimizer.SetRobots("noindex, nofollow");  // <meta name="robots" content="noindex, nofollow">
_optimizer.SetRobots("noarchive");          // <meta name="robots" content="noarchive">
```

The tag is not rendered unless one of these methods is called. `IsNoIndex()` reports whether the current directives include `noindex`.

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

### Breadcrumb Structured Data
The tag helper emits a `BreadcrumbList` JSON-LD script when any breadcrumbs are present. Global defaults added via `AddDefaultBreadcrumb()` are prepended to every request's trail; per-request crumbs are appended with `AddBreadCrumb()`, and `ClearBreadcrumbs()` removes everything including the defaults. Entries with blank titles are skipped while keeping positions consecutive, the final rendered crumb is treated as the current page (no link), relative crumb URLs are resolved against the base URL, and all values are safely encoded so titles containing HTML cannot break out of the script element.

### Sitemap
`ServeSitemap()` serves a cached sitemap at `/sitemap.xml` (configurable) built from all registered sitemap sources. Relative `Location` values are resolved against the base URL, entries are de-duplicated by URL, `priority` is clamped to the valid 0.0–1.0 range, and the result is cached for the configured duration (default 4 hours). Generation is guarded against cache stampedes, so concurrent first requests trigger only one refresh.

### RSS Feeds
`AddRssFeed()` registers an RSS 2.0 feed served at the given path. Each feed has its own title, description, item source function, and cache duration (default 2 hours). Item `Link` values should be absolute URLs; items without a title or link are skipped, and `Guid` defaults to the item's link. The channel `<link>` uses your configured base URL, which is why `WithBaseUrl()` is required. Multiple feeds can be registered at distinct paths, each cached and stampede-protected independently. Limiting the number of items is the caller's responsibility.

### robots.txt
`ServeRobotsTxt()` generates and serves a `robots.txt` at `/robots.txt` (configurable). The default output is:

```
User-agent: *
Allow: /

Sitemap: https://example.com/sitemap.xml
```

The `Sitemap:` line is added automatically if `ServeSitemap()` has been called, and omitted if not. Additional rules are appended after the default block using `AdditionalRules` — each string is one line, empty strings produce blank lines for spacing between blocks. This supports any valid robots.txt syntax including comments, multiple `User-agent` blocks, and `Crawl-delay`. Content is generated once and cached for the lifetime of the application.

### Static File Cache Middleware
The `StaticFileCacheHeaderMiddleware` applies `Cache-Control` headers to responses matching your configured `FileExtensions` and optional `Paths`. Path matching is segment-aware (`/js` matches `/js/app.js` but not `/jsx/app.js`) and case-insensitive. Headers are applied to `200 OK`, `304 Not Modified`, and `206 Partial Content` responses — including 304s ensures clients refresh their cache lifetime on revalidation, per RFC 9110. Headers are applied just before the response starts, so they take precedence over any headers set by `UseStaticFiles` (provided `UsePageOptimizer()` is registered first — see the ordering note above). Defaults when not configured: `.js`, `.css`, `.ico` extensions, `public`, 21-day max-age.