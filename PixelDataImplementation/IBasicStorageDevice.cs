using System;
using System.Collections.Generic;

using Philips.Platform.Common;
using Philips.Platform.Common.DataAccess;
using Philips.Platform.StorageDevices;
using Philips.Platform.StorageDevicesClient;


namespace PixelDataImplementation
{
    /// <summary>
    /// Query Sort Order
    /// </summary>
    public enum QuerySortOrder
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// Ascending order
        /// </summary>
        Ascending,
        /// <summary>
        /// Descending order
        /// </summary>
        Descending
    }

    /// <summary>
    /// A basic storage device supports query and fetch.
    /// </summary>
    public interface IBasicStorageDevice
    {
        /// <summary>
        /// Queries DICOM data from the storage device based on the filter
        /// </summary>
        /// <param name="level">
        /// The Query Level to be considered for returning the tags
        /// </param>
        /// <param name="parentIdentifier">
        /// parentIdentifier that restricts the query under its hierarchy
        /// </param>
        /// <param name="filters">
        /// Dicom tag value pair to be used as filters for query
        /// </param>
        /// <param name="sortCriteria"> Collection of Dictionary tags to be used for sorting
        /// purpose.
        /// </param>
        /// <param name="sortOrder"> Determines what is the sort order.</param>
        /// <param name="maxRecords"> 
        /// Limits the maximum number of results returned by query.
        /// </param>
        /// <param name="recordOffset"></param>
        /// <returns>
        /// An array of <see cref="DicomObject"/>. 
        /// <see cref="DicomObject"/> contains a <see cref="StorageKey"/> and
        /// <see cref="DicomObject"/>.
        /// </returns> 
        /// <exception cref="DataCorruptException">Data being read is corrupt and can't be read in
        /// a meaningful manner.</exception>
        QueryResult QueryData(
            QueryLevel level,
            Identifier parentIdentifier,
            QueryFilter filters,
            DictionaryTagsCollection sortCriteria = null,
            QuerySortOrder sortOrder = QuerySortOrder.None,
            int maxRecords = 200,
            int recordOffset = 0
        );

        /// <summary>
        /// Queries for images for a series from the storage device based on the filter
        /// </summary>
        /// <param name="parentIdentifier"></param>
        /// <param name="filters"></param>
        /// <returns></returns>
        IList<DicomObject> QueryImagesForSeries(
            Identifier parentIdentifier,
            QueryFilter filters);

        /// <summary>
        /// Check if study/series/image is present without returning 
        /// any query results
        /// </summary>
        /// <param name="level">The query level</param>
        /// <param name="parentIdentifier">The parent identifier</param>
        /// <param name="filters">The query filter</param>
        /// <returns></returns>
        bool IsObjectPresent(
            QueryLevel level,
            Identifier parentIdentifier,
            QueryFilter filters);

        /// <summary>
        /// Gets the number of studies that match the given <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">
        /// The filter to match.
        /// </param>
        /// <returns>
        /// The number of studies matching the filter.
        /// </returns>
        int GetNumberOfMatchingStudies(QueryFilter filter);

        /// <summary>
        /// Performs a non-blocking query operation of the list of devices provided.
        /// </summary>
        /// <param name="level">level</param>
        /// <param name="parentIdentifier">parent Identifier</param>
        /// <param name="filter">filter</param>
        /// <param name="state">state</param>
        void QueryAsync(
            QueryLevel level,
            Identifier parentIdentifier,
            QueryFilter filter,
            object state);

        /// <summary>
        /// Cancels Asynchronous query operation. Triggers a query completed Event.
        /// </summary>
        /// <param name="state">state</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to cancel an async twice and if previous cancel was complete.        
        /// </exception>
        /// <remarks>
        /// If cancel is called twice and the previous cancel operation is not complete, the 
        /// second cancel is ignored.
        /// </remarks>
        void CancelQueryAsync(object state);

        /// <summary>
        /// Gets the collection of instance storage keys for a given parent identifier.
        /// </summary>
        /// <param name="parentIdentifier">This can be study/series level identifier</param>
        /// <param name="filter">Dicom tag value pair to be used as filters for query</param>
        /// <returns></returns>
        IEnumerable<StorageKey> FindInstances(Identifier parentIdentifier,
            QueryFilter filter);

        /// <summary>
        /// Ensure that the pixel data and header information for the given
        /// image or frame is available on-disk, and return information about where these
        /// can be loaded from.
        /// </summary>
        /// <param name="identifiers">List of identifiers</param>
        /// <param name="loadingMode">loading mode</param>
        /// <returns>List of <see cref="FetchResult"/> containing the dcm file path</returns>
        /// <exception cref="DataCorruptException">Data being read is corrupt and can't be read in
        /// a meaningful manner.</exception>
        IList<FetchResult> Fetch(
            IList<Identifier> identifiers,
            Philips.Platform.StorageDevicesClient.DataLoadingMode loadingMode);

        /// <summary>
        /// Ensure that the pixel data and header information for the given
        /// image or frame is available on-disk. Asynchronously provides loading informations.
        /// </summary>
        /// <param name="identifiers">List of identifiers</param>
        /// <param name="userState">unique user state object to maintain call stack</param>
        /// <param name="loadingMode">Loading mode</param>
        void FetchAsync(
            IList<Identifier> identifiers,
            object userState,
            Philips.Platform.StorageDevicesClient.DataLoadingMode loadingMode);

        /// <summary>
        /// Cancels the Asynchronous fetch operation
        /// </summary>
        /// <param name="state">unique user state of the fetch to be cancelled</param>
        void CancelFetchAsync(object state);

        /// <summary>
        /// Fetches the list of fast headers for the specified list of image identifiers.
        /// NOTE: No frame headers are fetched in case of multi-frame image identifiers
        /// </summary>
        /// <param name="identifiers">List of image identifiers</param>
        /// <returns>List of image fast headers</returns>
        IList<FetchResult> FetchImageFastAccess(IList<Identifier> identifiers);

        /// <summary>
        /// Gets the children count.
        /// </summary>
        /// <param name="level">The query level.</param>
        /// <param name="parentIdentifier">The parent identifier.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>Children count for the parent storage key</returns>
        int GetChildrenCount(QueryLevel level, Identifier parentIdentifier, QueryFilter filter);

        /// <summary>
        /// Gets the SOP class count.
        /// </summary>
        /// <param name="parentIdentifier">The parent identifier.</param>
        /// <returns>
        /// Dictionary of SOP class Uid and Count of occurrences.
        /// </returns>
        IDictionary<string, int> GetSopClassInformation(Identifier parentIdentifier);

        /// <summary>
        /// Event to obtain the intermediate progress of fetch Operation.
        /// </summary>
        event EventHandler<FetchProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Event to obtain the completed status of fetch Operation.
        /// </summary>
        event EventHandler<FetchCompletedEventArgs> Completed;

        /// <summary>
        /// Event to obtain the intermediate progress of Query Operation.
        /// </summary>
        event EventHandler<DeviceQueryProgressEventArgs> AIPQueryProgressChanged;

        /// <summary>
        /// Event to obtain the completed status of Query Operation.
        /// </summary>
        event EventHandler<DeviceQueryCompletedEventArgs> AIPQueryCompleted;

        /// <summary>
        /// Returns configuration associated with current storage device.
        /// </summary>
        object Configuration { get; }

        /// <summary>
        /// Returns storage device id.
        /// </summary>
        string DeviceID { get; set; }

        /// <summary>
        /// Check if the full header file exists
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        bool DoesFullHeaderFileExists(Identifier identifier);
    }
}