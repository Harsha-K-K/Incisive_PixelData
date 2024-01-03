using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// Translation status
    /// </summary>
    internal enum ImageTranslationStatus
    {
        /// <summary>
        /// Image is OK (no conversion needed)
        /// </summary>
        ShouldNotBeTranslated,

        /// <summary>
        /// Image should be converted by ImageTranlator
        /// </summary>
        ShouldBeTranslated,

        /// <summary>
        /// Image is not supported (ImageTranslator cannot convert it)
        /// Bad images (e.g.missing critical attributes) or we don't know how to convert the image
        /// </summary>
        NotSupported
    }
}
