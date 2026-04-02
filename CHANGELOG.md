# CHANGELOG

<!-- markdownlint-disable MD034 -->

<!-- next-release -->

## 1.3.0
- Added `SetMetaImage()` and `WithDefaultImage()` for og:image and twitter:image support
- Added `SetOgType()` for configurable og:type (e.g. "article", "product")
- Added `SetTwitterCard()` with `TwitterCard` enum (`Summary`, `SummaryLargeImage`, `App`, `Player`)
- Fixed breadcrumb `@id` values in JSON-LD structured data — previously used relative URLs, now correctly resolved to absolute URLs
- Fixed `SitemapService` not receiving configured cache duration or path — options were stored in `PageOptimizerConfig` but never reached the service
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