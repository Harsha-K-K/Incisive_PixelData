using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    //
    // Summary:
    //     PixelDataType
    public enum PixelDataType
    {
        None,
        FileOffsetLength,
        DicomFile,
        //
        // Summary:
        //     Represents a WADO Image
        Wado,
        //
        // Summary:
        //     Represents fast-repository in-memory image.
        InMemory,
        //
        // Summary:
        //     Represents a Amazon HealthLake Imaging Image
        Ahli
    }
}
