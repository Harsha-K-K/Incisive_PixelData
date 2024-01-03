using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    //
    // Summary:
    //     Represents the type of the objects that can be stored.
    internal enum EntityType
    {
        //
        // Summary:
        //     None
        None,
        //
        // Summary:
        //     Study
        Study,
        //
        // Summary:
        //     Series
        Series,
        //
        // Summary:
        //     SF Image
        SFImage,
        //
        // Summary:
        //     MF Image
        MFImage,
        //
        // Summary:
        //     Leaf
        Leaf,
        //
        // Summary:
        //     Frame
        Frame,
        //
        // Summary:
        //     Unknown entity type.
        Unknown
    }
}
