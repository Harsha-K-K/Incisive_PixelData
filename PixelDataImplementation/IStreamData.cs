using Philips.Platform.Common;
using Philips.Platform.StorageDevicesClient;
using System.Collections.Generic;
using System.ServiceModel;

namespace PixelDataImplementation
{
    /// <summary>
    /// Defines the methods for Database service operations
    /// </summary>
    [ServiceContract]
    public interface IStreamData
    {
        /// <summary>
        /// Ensure that the pixel data and header information for the given
        /// image or frame is available on-disk, and return information about where these
        /// can be loaded from.
        /// </summary>
        /// <param name="identifiers">The list of identifiers</param>
        /// <param name="loadingMode">The data loading mode</param>
        /// <param name="skipLoadingFrames">Skip loading frame data</param>
        /// <returns>
        /// The list of <see cref="FetchInformation"/> containing image information at series level
        /// </returns>
        /// <exception cref="Philips.Platform.InfraClient.Foundation.FailException">
        /// In case of client/service deployment, this operation may throw fail exception.
        /// </exception>
        /// <exception cref="DataCorruptException">
        /// Data being read is corrupt and can't be read in a meaningful manner.
        /// </exception>
        IList<FetchInformation> FetchAsStream(
            IList<Identifier> identifiers,
            DataLoadingMode loadingMode,
            bool skipLoadingFrames);
    }

}