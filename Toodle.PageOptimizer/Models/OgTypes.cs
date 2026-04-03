using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Encodings.Web;

namespace Toodle.PageOptimizer.Models
{
    /// <summary>
    /// Base class for Open Graph type objects. Pass an instance to SetOgType() to set
    /// the og:type meta tag and render any type-specific properties.
    /// Extend this class to define custom OgTypes.
    /// </summary>
    public abstract class OgType
    {
        /// <summary>The og:type value written to the meta tag (e.g. "article", "website").</summary>
        public abstract string TypeName { get; }

        /// <summary>
        /// Renders any type-specific meta tags into the provided list.
        /// Override in custom subclasses to emit additional properties.
        /// </summary>
        /// <param name="metaTags">The list of HTML meta tag strings to append to.</param>
        public virtual void RenderTags(List<string> metaTags) { }
    }

    /// <summary>
    /// Represents a generic website page. Renders og:type="website" with no additional properties.
    /// </summary>
    public class OgTypeWebsite : OgType
    {
        public override string TypeName => "website";
    }

    /// <summary>
    /// Represents an article page such as a blog post or news story.
    /// Renders og:type="article" along with article-specific meta tags.
    /// </summary>
    public class OgTypeArticle : OgType
    {
        public override string TypeName => "article";

        /// <summary>The date and time the article was first published. Optional.</summary>
        public DateTime? PublishedTime { get; set; }

        /// <summary>The date and time the article was last modified. Optional.</summary>
        public DateTime? ModifiedTime { get; set; }

        /// <summary>The author of the article. Optional.</summary>
        public string? Author { get; set; }

        /// <summary>The section or category the article belongs to (e.g. "Technology"). Optional.</summary>
        public string? Section { get; set; }

        /// <summary>Tags associated with the article. Optional.</summary>
        public IEnumerable<string>? Tags { get; set; }

        public override void RenderTags(List<string> metaTags)
        {
            if (PublishedTime.HasValue)
                metaTags.Add($"<meta property=\"article:published_time\" content=\"{PublishedTime.Value:o}\" />");

            if (ModifiedTime.HasValue)
                metaTags.Add($"<meta property=\"article:modified_time\" content=\"{ModifiedTime.Value:o}\" />");

            if (!string.IsNullOrWhiteSpace(Author))
                metaTags.Add($"<meta property=\"article:author\" content=\"{HtmlEncoder.Default.Encode(Author)}\" />");

            if (!string.IsNullOrWhiteSpace(Section))
                metaTags.Add($"<meta property=\"article:section\" content=\"{HtmlEncoder.Default.Encode(Section)}\" />");

            if (Tags != null)
            {
                foreach (var tag in Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                        metaTags.Add($"<meta property=\"article:tag\" content=\"{HtmlEncoder.Default.Encode(tag)}\" />");
                }
            }
        }
    }

    /// <summary>
    /// Represents a product page. Renders og:type="product" along with product-specific meta tags.
    /// Note: product is a widely-used Facebook extension rather than part of the official OG spec.
    /// </summary>
    public class OgTypeProduct : OgType
    {
        public override string TypeName => "product";

        /// <summary>The price of the product. Optional.</summary>
        public decimal? PriceAmount { get; set; }

        /// <summary>The ISO 4217 currency code for the price (e.g. "GBP", "USD"). Optional.</summary>
        public string? PriceCurrency { get; set; }

        /// <summary>The availability of the product (e.g. "instock", "oos", "pending"). Optional.</summary>
        public string? Availability { get; set; }

        public override void RenderTags(List<string> metaTags)
        {
            if (PriceAmount.HasValue)
                metaTags.Add($"<meta property=\"product:price:amount\" content=\"{PriceAmount.Value.ToString("F2", CultureInfo.InvariantCulture)}\" />");

            if (!string.IsNullOrWhiteSpace(PriceCurrency))
                metaTags.Add($"<meta property=\"product:price:currency\" content=\"{HtmlEncoder.Default.Encode(PriceCurrency)}\" />");

            if (!string.IsNullOrWhiteSpace(Availability))
                metaTags.Add($"<meta property=\"product:availability\" content=\"{HtmlEncoder.Default.Encode(Availability)}\" />");
        }
    }

    /// <summary>
    /// Represents a user profile page. Renders og:type="profile" along with profile-specific meta tags.
    /// </summary>
    public class OgTypeProfile : OgType
    {
        public override string TypeName => "profile";

        /// <summary>The person's first name. Optional.</summary>
        public string? FirstName { get; set; }

        /// <summary>The person's last name. Optional.</summary>
        public string? LastName { get; set; }

        /// <summary>The person's username or handle. Optional.</summary>
        public string? Username { get; set; }

        public override void RenderTags(List<string> metaTags)
        {
            if (!string.IsNullOrWhiteSpace(FirstName))
                metaTags.Add($"<meta property=\"profile:first_name\" content=\"{HtmlEncoder.Default.Encode(FirstName)}\" />");

            if (!string.IsNullOrWhiteSpace(LastName))
                metaTags.Add($"<meta property=\"profile:last_name\" content=\"{HtmlEncoder.Default.Encode(LastName)}\" />");

            if (!string.IsNullOrWhiteSpace(Username))
                metaTags.Add($"<meta property=\"profile:username\" content=\"{HtmlEncoder.Default.Encode(Username)}\" />");
        }
    }
}
