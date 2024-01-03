using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// Provides fast repository service related utility APIs.
    /// </summary>
    internal interface IFastRepositoryServiceHelper
    {
        bool IsFastRepositoryServiceInstalled();
    }

    /// <summary>
    /// Provides an implementation of <see cref="IFastRepositoryServiceHelper"/>
    /// </summary>
    //@AdapterType: Service
    internal class FastRepositoryServiceHelper : IFastRepositoryServiceHelper
    {
        /// <summary>
        /// <see cref="IFastRepositoryServiceHelper.IsFastRepositoryServiceInstalled"/>
        /// </summary>
        public bool IsFastRepositoryServiceInstalled()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == "IPF-FastInMemoryRepositoryService");
        }
    }
}
