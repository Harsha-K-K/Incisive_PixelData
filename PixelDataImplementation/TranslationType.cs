using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    public enum TranslationType
    {
        /// <summary>
        /// Converts the signed to unsigned 
        /// </summary>
        Regular = 0x00,
        /// <summary>
        /// Makes the negative values 0
        /// </summary>
        Truncated = 0x01,
        /// <summary>
        /// Does not convert signed to unsgined
        /// </summary>
        None = 0x02
    };
}
