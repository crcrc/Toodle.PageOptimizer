namespace Toodle.PageOptimizer.Models
{
    /// <summary>Specifies the Twitter Card type for the twitter:card meta tag.</summary>
    public enum TwitterCard
    {
        /// <summary>A small square thumbnail image with a title and description.</summary>
        Summary,
        /// <summary>A large full-width image with a title and description. Recommended for articles and products.</summary>
        SummaryLargeImage,
        /// <summary>A card that links to a mobile app.</summary>
        App,
        /// <summary>A card that embeds a video or audio player.</summary>
        Player
    }
}
