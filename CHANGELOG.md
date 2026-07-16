# CHANGELOG

<!-- markdownlint-disable MD034 -->

<!-- next-release -->
## 1.5.3
- Fixed `Link` header emission for URLs containing commas (e.g. Cloudinary transformation URLs like `f_auto,q_auto,w_1920`) — `AppendCommaSeparatedValues` wrapped such link-values in double quotes, which is invalid RFC 8288 syntax, so browsers/CDNs dropped the entry. The header value is now joined manually; commas inside `<...>` are unambiguous

## 1.5.2
- Misc Bug fixes
- 
## 1.5.1
- Misc Bug fixes

## 1.5.0
- Added `AddRssFeed()` to generate an RSS feed at a specified endpoint. Supports custom feed items via `IEnumerable<RssFeedItem>` and auto-generates required XML structure with appropriate headers.

## 1.4.0
- Added `SetRobots()` for full robots meta tag control (e.g. `"noindex, nofollow"`, `"noarchive"`). `SetNoIndex()` retained as a shortcut for `SetRobots("noindex")`
- Added `ServeRobotsTxt()` to serve a generated robots.txt. Supports custom rules via `IEnumerable<string>`. `Sitemap:` line added automatically if `ServeSitemap()` is configured

## 1.3.0
- Added `SetMetaImage()` and `WithDefaultImage()` for og:image and twitter:image support. Optional `width`, `height`, and `alt` parameters render `og:image:width`, `og:image:height`, `og:image:alt`, and `twitter:image:alt`
- Added `SetOgType()` accepting typed objects (`OgTypeArticle`, `OgTypeProduct`, `OgTypeProfile`, `OgTypeWebsite`) that render og:type and their type-specific properties automatically. Extensible via `OgType` base class
- Added `SetTwitterCard()` with `TwitterCard` enum (`Summary`, `SummaryLargeImage`, `App`, `Player`)
- Fixed breadcrumb `@id` values in JSON-LD structured data — previously used relative URLs, now correctly resolved to absolute URLs
- Fixed `SitemapService` not receiving configured cache duration — options were stored in `PageOptimizerConfig` but never wired to the service via `IOptions<SitemapOptions>`
- Fixed `StaticFileCacheHeaderMiddleware` — headers are now set via `OnStarting` callback so they take precedence over `UseStaticFiles`, and are only applied to 200 responses

## 1.2.1
- Fixed Sitemap.xml encoding
- Added dotnet 10 build

## 1.2.0
- Added Sitemap generation middleware

## 1.1.0
- Refactored to use a fluid configuration API
- Added '.AddStaticFileCacheHeaders()' to add sane default cache-control headers to static files

## 1.0.0
- Initial release