using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// Mmf details for pixel information stored in-memory.
    /// </summary>
    [Serializable]
    public class InMemoryPixelInformation
    {
        /// <summary>
        /// PixelMmfName
        /// </summary>
        public string PixelMmfName { get; set; }

        /// <summary>
        /// Offset information for current pixel
        /// </summary>
        public long PixelOffset { get; set; }


        /// <summary>
        /// PixelLength
        /// </summary>
        public long PixelLength { get; set; }
    }
}
