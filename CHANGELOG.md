# CHANGELOG

<!-- markdownlint-disable MD034 -->

<!-- next-release -->
## 1.6.0
- Added `ClearRobots()` to reset the robots directives for the current page, undoing a previous `SetRobots()`/`SetNoIndex()` so that no robots meta tag is rendered. `SetRobots()` rejects an empty string, so there was previously no way back to the unset state. This matters wherever the scoped service outlives a single page — in a Blazor interactive circuit every page shares one instance, so a component calling `SetNoIndex()` conditionally would leave `noindex` set for every page rendered afterwards in that circuit. `ClearBreadcrumbs()` already covered the equivalent case for breadcrumbs

## 1.5.5
- Fixed RSS `<pubDate>` rendering an invalid date — `GMT` was unquoted in the format string, so `M` was interpreted as the month specifier and July formatted as `09:15:00 G7T`. Feed readers rejected the feed as invalid RFC 822
- Fixed `PublishedDate` values with `DateTimeKind.Unspecified` being shifted by the server's UTC offset. EF Core returns `Unspecified` even for columns storing UTC, so database-sourced dates were converted as if they were local time. `Unspecified` is now taken to already be UTC; `Local` and `Utc` values are converted as before
- Fixed `Link` headers not being emitted for requests that do not advertise `text/html` in their `Accept` header (`HEAD` probes, `fetch()` navigations, htmx, CDNs). Eligibility is now decided at response start from the response's `Content-Type` rather than from the request, and `HEAD` requests are included. As a result `Link` headers also no longer leak onto RSS or JSON responses served from extensionless routes

## 1.5.4
- Renamed from Toodle.PageOptimizer to Admirably.PageOptimizer


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