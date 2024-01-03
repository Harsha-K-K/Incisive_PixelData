using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// The file type (where the data to be retrieved is stored in)
    /// </summary>
    [DataContract]
    internal enum PixelSource
    {

        /// <summary>
        /// The data is retrieved from a physical file on disk
        /// </summary>
        [EnumMember]
        File,

        /// <summary>
        /// The data is retrieved from a wado url.
        /// </summary>
        [EnumMember]
        WadoUri,

        /// <summary>
        /// The data is retrieved from a Amazon HealthLake Imaging.
        /// </summary>
        [EnumMember]
        Ahli
    }
}
