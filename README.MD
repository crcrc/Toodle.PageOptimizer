# Toodle.PageOptimizer

A metadata, breadcrumb and resource manager for ASP.NET Core websites.

## Installation

```bash
dotnet add package Toodle.PageOptimizer
```

## Usage

1. Add the service in Program.cs:
```csharp
builder.Services.AddMetaData(
    siteName: "My Website",
    baseUrl: "https://example.com",
    locale: "en-US"
);
```

2. Add the tag helper in _ViewImports.cshtml:
```csharp
@addTagHelper *, MetaData.AspNetCore
```

3. Use the tag helper in your views:
```html
<head>
    <meta-data-tags />
</head>
```

4. Use the service in your controllers:
```csharp
public class ArticleController : Controller
{
    private readonly IMetaDataService _metaDataService;

    public ArticleController(IMetaDataService metaDataService)
    {
        _metaDataService = metaDataService;
    }

    public IActionResult Article(string slug)
    {
        _metaDataService
            .SetMetaTitle("Article Title")
            .SetMetaDescription("Article description")
            .AddBreadCrumb("Category", "/category")
            .AddBreadCrumb("Article Title")
            .SetCanonicalUrl($"/article/{slug}");

        return View();
    }
}
```