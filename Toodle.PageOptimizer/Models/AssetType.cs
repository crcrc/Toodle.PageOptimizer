using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Admirably.PageOptimizer.Models
{
    /// <summary>Specifies the type of resource for a rel=preload Link header hint.</summary>
    public enum AssetType
    {
        /// <summary>A CSS stylesheet.</summary>
        Style,
        /// <summary>A JavaScript file.</summary>
        Script,
        /// <summary>A web font file.</summary>
        Font,
        /// <summary>An image file.</summary>
        Image,
        /// <summary>An audio file.</summary>
        Audio,
        /// <summary>A video file.</summary>
        Video,
        /// <summary>An HTML document.</summary>
        Document,
        /// <summary>A resource to be fetched via the Fetch API.</summary>
        Fetch
    }
}
