using Philips.Platform.Common;


namespace PixelDataImplementation
{
    /// <summary>
    /// Contains methods to Create new StoreSessions.
    /// </summary>
    internal interface IDeviceStore
    {

        /// <summary>
        /// Creates a new ICompositeStoreSession Instance.
        /// </summary>
        /// <returns>an instance of <see cref="ICompositeStoreSession"/></returns>
        ICompositeStoreSession CreateCompositeStoreSession();

        /// <summary>
        /// Method to create a Store session for storing frames under a single Multi-Frame image. 
        /// </summary>
        /// <param name="commonHeader">
        /// Dicom Object containing the data common to all frames. 
        /// This includes attributes corresponding to Study, Series and Image level.
        /// The attributes corresponding to frames shall be stored 
        /// using the MultiFrameStoreSession object.
        /// </param>
        /// <returns>an instance of <see cref="IMultiFrameStoreSession"/></returns>
        IMultiFrameStoreSession CreateMultiFrameStoreSession(DicomObject commonHeader);

        /// <summary>
        /// Gets the staging area for the device from StorageDevicesManager.
        /// </summary>
        /// <returns></returns>
        string GetStagingAreaLocation();
    }
}
