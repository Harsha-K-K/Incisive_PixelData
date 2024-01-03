using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.ApplicationIntegration.Tracing;
using Philips.Platform.Common;
using Philips.Platform.Common.DataAccess;
using Philips.Platform.CommonUtilities.Pooling;
using Philips.Platform.StorageDevicesClient;
using Philips.Platform.SystemIntegration.Exceptions;

using DicomObject = Philips.Platform.Common.DicomObject;

namespace PixelDataImplementation
{
    /// <summary>
    /// This abstract class provides basic functionality of a local SQL based StorageDevice.
    /// </summary>
    internal abstract class StorageDeviceBase :
        IBasicStorageDevice,
        IDeviceStore,
        IDataDelete,
        IApplicationBlob,
        IStreamData
    {

        private QueryHelper queryHelper;

        private FetchHelper fetchHelper;

        private DeleteHelper deleteHelper;

        /// <summary>
        /// Instance for DeleteProtection.
        /// </summary>
        private DeleteProtection deleteProtection;

        private BlobHelperBase applicationBlobHelper;

        private static readonly Tracer tracer =
            Tracer.CreateTracer(typeof(StorageDeviceBase));
        private static readonly PerformanceTracer performanceTracer =
            PerformanceTracer.CreatePerformanceTracer(typeof(StorageDeviceBase));
        private static readonly PredefinedAction deleteAction =
            new PredefinedAction("ForceDeleteAction");

        ///<summary>
        /// Whether or not the async operation referring the state is cancelled.
        /// </summary>
        private readonly Dictionary<object, bool> stateToIsCancelled =
            new Dictionary<object, bool>();

        internal string deviceId;

        /// <summary>
        /// QueryHelper instance of StorageDevice.
        /// </summary>
        public QueryHelper QueryHelper { get { return queryHelper; } }

        /// <summary>
        /// FetchHelper instance of storageDevice
        /// </summary>
        public FetchHelper FetchHelper { get { return fetchHelper; } }

        /// <summary>
        /// Event to obtain the intermediate progress of fetch Operation.
        /// </summary>
        public event EventHandler<FetchProgressChangedEventArgs> ProgressChanged =
            delegate { };

        /// <summary>
        /// Event to obtain the completed status of fetch Operation.
        /// </summary>
        public event EventHandler<FetchCompletedEventArgs> Completed = delegate { };

        /// <summary>
        /// Device ID.
        /// </summary>
        public string DeviceID
        {
            get
            {
                return deviceId;
            }
            set
            {
                deviceId = value;
            }
        }

        /// <summary>
        /// DatabaseStore instance of StorageDevice.
        /// </summary>
        public IDatabaseStore DatabaseStore { get; internal set; }

        /// <summary>
        /// DatabaseStore instance of StorageDevice.
        /// </summary>
        public DeviceConfiguration DatabaseStoreConfiguration { get; internal set; }

        /// <summary>
        /// FileStore instance of StorageDevice.
        /// </summary>
        public FileStoreBase FileStore { get; internal set; }

        /// <summary>
        /// Initializes a new StorageDeviceBase
        /// </summary>
        /// <param name="deviceId">the associated storage device ID</param>
        protected StorageDeviceBase(string deviceId)
        {
            this.deviceId = deviceId;
        }

        /// <summary>
        /// Initializes the with specified values.
        /// </summary>
        /// <param name="databaseStore"></param>
        /// <param name="databaseStoreConfig"></param>
        public void Init(
            IDatabaseStore databaseStore,
            DeviceConfiguration databaseStoreConfig
        )
        {
            DatabaseStore = databaseStore;
            DatabaseStore.Connect();
            DatabaseStoreConfiguration = databaseStoreConfig;

            if (FileStore == null)
            {
                FileStore = FileStoreBase.CreateFileStore(deviceId);
            }

            if (queryHelper == null)
            {
                queryHelper = new QueryHelper(DatabaseStore, FileStore);
            }

            if (fetchHelper == null)
            {
                fetchHelper = new FetchHelper(deviceId, queryHelper, FileStore);
            }

            if (deleteHelper == null)
            {
                deleteHelper = new DeleteHelper(deviceId, FileStore, DatabaseStore);
            }

            if (
                deleteProtection == null
            )
            {
                deleteProtection = new DeleteProtection(deviceId);
            }

            applicationBlobHelper = new ApplicationBlobHelper(deviceId);
        }

        /// <summary>
        /// Close the database connection and release any resources
        /// </summary>
        public void ReleaseResources()
        {
            DatabaseStore.Close();
            DatabaseStore = null;
            FileStore = null;
            queryHelper = null;
            fetchHelper = null;

            deleteHelper = null;
            applicationBlobHelper = null;
        }

        #region Abstract Methods

        /// <summary>
        /// Abstract method to close all device connection
        /// </summary>
        public abstract void CloseConnection();

        /// <summary>
        /// Abstract method to initialize the storage device.
        /// </summary>
        public abstract void Init();

        #endregion Abstract Methods

        #region IBasicStorageDevice Members

        /// <summary>
        /// Gets the children count.
        /// </summary>
        /// <param name="level">The query level</param>
        /// <param name="parentIdentifier">The parent identifier</param>
        /// <param name="filter">The query filter</param>
        /// <returns></returns>
        public int GetChildrenCount(
            QueryLevel level,
            Identifier parentIdentifier,
            QueryFilter filter
        )
        {
            return queryHelper.GetChildrenCount(level, parentIdentifier, filter);
        }

        /// <summary>
        /// Gets the SOP class count.
        /// </summary>
        /// <param name="parentIdentifier">The parent identifier</param>
        /// <returns>
        /// Dictionary of SOP class Uid and Count of occurrences.
        /// </returns>
        public IDictionary<string, int> GetSopClassInformation(Identifier parentIdentifier)
        {
            return queryHelper.GetSopClassInformation(parentIdentifier);
        }

        /// <summary>
        /// See <see cref="IBasicStorageDevice.QueryData"/>
        /// </summary>
        public QueryResult QueryData(
            QueryLevel level,
            Identifier parentIdentifier,
            QueryFilter filter,
            DictionaryTagsCollection sortCriteria = null,
            QuerySortOrder sortOrder = QuerySortOrder.None,
            int maxRecords = 200,
            int recordOffset = 0
        )
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.QueryData");
            QueryResult queryData =
                queryHelper.QueryData(
                    level,
                    parentIdentifier,
                    filter,
                    sortCriteria,
                    sortOrder,
                    maxRecords,
                    recordOffset
                );
            tracer.TraceVerbose("Exiting StorageDeviceBase.QueryData");
            return queryData;
        }

        /// <summary>
        /// Checks if the given object identified by the <paramref name="parentIdentifier"/>
        /// and <paramref name="filter"/> is present in the storage device.
        /// </summary>
        public bool IsObjectPresent(
            QueryLevel level,
            Identifier parentIdentifier,
            QueryFilter filter)
        {
            return queryHelper.IsObjectPresent(level, parentIdentifier, filter);
        }

        /// <summary>
        /// Check if full header exists
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public bool DoesFullHeaderFileExists(Identifier identifier)
        {
            return FetchHelper.DoesFullHeaderFileExists(new StorageKey(deviceId, identifier));
        }


        /// <summary>
        /// Gets the number of studies that match the <paramref name="filter"/>.
        /// </summary>
        public int GetNumberOfMatchingStudies(QueryFilter filter)
        {
            return DatabaseStore.GetNumberOfMatchingRecords(
                Level.Study,
                null, //null parent id since query is at study level
                filter
            );
        }

        /// <summary>
        /// Queries for images for a series from the storage device based on the filter
        /// </summary>
        /// <param name="parentIdentifier">
        /// parentIdentifier that restricts the query under its hierarchy
        /// </param>
        /// <param name="filters">
        /// Dicom tag value pair to be used as filters for query
        /// </param>
        /// <returns>
        /// An array of <see cref="DicomObject"/>. 
        /// </returns> 
        public IList<DicomObject> QueryImagesForSeries(
            Identifier parentIdentifier,
            QueryFilter filters
        )
        {
            return queryHelper.QuerImagesForSeries(parentIdentifier, filters);
        }

        /// <inheritdoc />
        /// <summary>
        /// See <see cref="IBasicStorageDevice.FindInstances(Identifier, QueryFilter)" />
        /// </summary>
        public IEnumerable<StorageKey> FindInstances(
            Identifier parentIdentifier, QueryFilter filter)
        {
            return queryHelper.FindInstances(parentIdentifier, filter);
        }

        /// <summary>
        /// </summary>
        /// <param name="identifiers"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public IList<FetchResult> Fetch(IList<Identifier> identifiers, DataLoadingMode mode)
        {
            return fetchHelper.Fetch(identifiers, mode);
        }

        /// <summary>
        /// Fetches the list of image fast headers for the specified list of image identifiers.
        /// NOTE: No frame headers are fetched in case of multi-frame image identifiers
        /// </summary>
        /// <param name="identifiers">List of image identifiers</param>
        /// <returns>List of image fast headers</returns>
        public IList<FetchResult> FetchImageFastAccess(IList<Identifier> identifiers)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.FetchImageFastAccess");
            List<FetchResult> fetchResults;
            fetchResults = fetchHelper.FetchImageFastAccess(identifiers);
            tracer.TraceVerbose("Exiting StorageDeviceBase.FetchImageFastAccess");
            return fetchResults;
        }

        /// <summary>
        /// Asynchronously provides the fetch results of loaded identifier data.
        /// </summary>
        /// <param name="identifiers">List of identifiers to fetch</param>
        /// <param name="userState">state object to maintain call stack</param>
        /// <param name="loaindMode">Loading mode</param>
        public void FetchAsync(
            IList<Identifier> identifiers,
            object userState,
            DataLoadingMode loaindMode
        )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// See <see cref="IBasicStorageDevice.CancelFetchAsync"/>
        /// </summary>
        public void CancelFetchAsync(object state)
        {
            throw new NotImplementedException();
        }

        ///<summary>
        /// See 
        /// <see cref="IBasicStorageDevice.QueryAsync(QueryLevel, Identifier, QueryFilter, object)"
        /// />
        /// </summary>
        public void QueryAsync(
            QueryLevel level,
            Identifier parentIdentifier,
            QueryFilter filter,
            object state
        )
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.QueryAsync");
            //TODO: To refactor this when native TPL implementation is done.
            lock (stateToIsCancelled)
            {
                if (stateToIsCancelled.ContainsKey(state))
                {
                    const string errorMessage =
                        "QueryAsync could not be performed. The state information passed " +
                        "is already being processed. Query using a unique state information.";
                    StorageDevicesUtilities.LogError(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                stateToIsCancelled.Add(state, false);
            }

            QueryAsyncDelegate asyncWorkerThread = AsynchronousQuery;
            asyncWorkerThread.BeginInvoke(
                level,
                parentIdentifier,
                filter,
                state,
                null, //AsynchronousQueryCallback
                null);
            tracer.TraceVerbose("Exiting StorageDeviceBase.QueryAsync");
        }

        /// <summary>
        /// See <see cref="IBasicStorageDevice.CancelQueryAsync(object)"/>
        /// </summary>
        public void CancelQueryAsync(object state)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.CancelQueryAsync");
            lock (stateToIsCancelled)
            {
                // If the value is not available in stateToIsCancelled, it means that the async
                // query is no more an active query.
                if (!stateToIsCancelled.ContainsKey(state))
                {
                    const string errorMessage = "The state information provided is invalid.";
                    StorageDevicesUtilities.LogError(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                stateToIsCancelled[state] = true;
            }

            DeviceQueryCompletedEventArgs completedArgs =
                new DeviceQueryCompletedEventArgs(null, true, state, null);

            OnQueryCompleted(completedArgs);
            tracer.TraceVerbose("Exiting StorageDeviceBase.CancelQueryAsync");
        }

        /// <summary>
        /// See <see cref="IBasicStorageDevice.AIPQueryProgressChanged"/>
        /// </summary>
        public event EventHandler<DeviceQueryProgressEventArgs> AIPQueryProgressChanged;

        /// <summary>
        /// See <see cref="IBasicStorageDevice.AIPQueryCompleted"/>
        /// </summary>
        public event EventHandler<DeviceQueryCompletedEventArgs> AIPQueryCompleted;

        /// <summary>
        /// See <see cref="IBasicStorageDevice.Configuration"/>
        /// </summary>
        public object Configuration
        {
            get
            {
                return DatabaseStoreConfiguration;
            }
        }
        #endregion

        #region IDeviceStore Members

        /// <summary>
        /// See <see cref="IDeviceStore.CreateCompositeStoreSession()"/>
        /// </summary>
        public ICompositeStoreSession CreateCompositeStoreSession()
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.CreateCompositeStoreSession");
            ICompositeStoreSession storeSession = new CompositeStoreSession(deviceId, FileStore);

            tracer.TraceVerbose("Exiting StorageDeviceBase.CreateCompositeStoreSession");
            return storeSession;
        }

        /// <summary>
        /// See <see cref="IDeviceStore.CreateMultiFrameStoreSession(DicomObject)"/>
        /// </summary>
        public IMultiFrameStoreSession CreateMultiFrameStoreSession(DicomObject commonHeader)
        {
            tracer.TraceVerbose("Executing StorageDeviceBase.CreateMultiFrameStoreSession");
            return new MultiFrameStoreSession(
                deviceId, /*FileStore,*/ StorageDevicesUtilities.GetAipDicomObject(commonHeader)
            );
        }

        /// <summary>
        /// See <see cref="IDeviceStore.GetStagingAreaLocation()"/>
        /// </summary>
        public string GetStagingAreaLocation()
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.GetStagingAreaLocation");
            string stagingAreaLocation = ConfigurationManager.Instance.GetStagingAreaDirectory();
            tracer.TraceVerbose("Exiting StorageDeviceBase.GetStagingAreaLocation");
            return stagingAreaLocation;
        }

        #endregion

        #region IDataDelete

        /// <summary>
        /// See <see cref="IDataDelete.DeleteData(Level, Identifier)"/>
        /// </summary>
        public void DeleteData(Level level, Identifier identifier)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.DeleteData");

            if (!CanDelete(identifier))
            {
                throw new ObjectInUseException("The identifier : " +
                    identifier.Anonymize() +
                    " is delete protected or in use and cannot be deleted.");
            }
            deleteHelper.DeleteData(level, identifier);
            tracer.TraceVerbose("Exiting StorageDeviceBase.DeleteData");
        }

        /// <summary>
        /// See <see cref="IDataDelete.ForceDeleteImagesUnderSeries"/>
        /// </summary>        
        public void ForceDeleteImagesUnderSeries(IList<Identifier> imageIdentifiers)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.ForceDeleteImagesUnderSeries");

            if (imageIdentifiers == null)
            {
                throw new ArgumentNullException("imageIdentifiers");
            }

            foreach (var id in imageIdentifiers)
            {
                if (id == null || string.IsNullOrWhiteSpace(id.SopInstanceUid))
                {
                    throw new ArgumentException("One of the image identifiers is either " +
                        "null or not a valid image identifier.");
                }
            }

            if (imageIdentifiers.Count > 0)
            {
                if (!CanDelete(imageIdentifiers[0]))
                {
                    throw new ObjectInUseException("The identifier : " +
                        imageIdentifiers[0].Anonymize() +
                        " is delete protected or in use and cannot be deleted.");
                }
            }

            deleteHelper.DeleteImages(imageIdentifiers);
            //StorageDevicesManager.ForceCleanup(DeviceID);
            tracer.TraceVerbose("Exiting StorageDeviceBase.ForceDeleteImagesUnderSeries");
        }

        /// <summary>
        /// Deletes data with specified parameters.
        /// </summary>
        public void DeleteData(Level level, Identifier identifier, bool onlyTableData)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.DeleteData");

            if (!CanDelete(identifier))
            {
                throw new ObjectInUseException("The identifier : " +
                    identifier.Anonymize() +
                    " is delete protected or in use and cannot be deleted.");
            }
            deleteHelper.DeleteData(level, identifier, onlyTableData);
            tracer.TraceVerbose("Exiting StorageDeviceBase.DeleteData");
        }

        /// <summary>
        /// See <see cref="IDataDelete.ForceDeleteData(Level, Identifier, bool)"/>
        /// </summary>
        public void ForceDeleteData(
            Level level, Identifier identifier, bool propagateDeleteUpwards
        )
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.ForceDeleteData");
            PerformanceTraceToken deleteToken = null;

            try
            {
                deleteToken = performanceTracer.TraceBeginInfo(
                    deleteAction, "ForceDeleteData");

                switch (level)
                {
                    case Level.Study:
                        if (!IsStudyLocked(new StorageKey(deviceId, identifier)))
                        {
                            deleteHelper.DeleteData(
                                level,
                                identifier,
                                false,
                                propagateDeleteUpwards
                                );
                        }
                        break;
                    case Level.Series:
                        //TODO: This is a band-aid approach to force deleting a series.
                        //Note that we have no mechanism to delete protect a series
                        //and hence there is no delete protect file created at the series level.
                        //Till that time we have explicit APIs to
                        //delete protect/unprotect a series,
                        //we will just delete the data at the series level without checking for
                        //any protection whatsoever at any level above or below the series.
                        //
                        //TODO: While introducing series level delete protection/unpotection APIs
                        //also have consideration for image level delete protect/unprotect
                        //because MR has a use case for that. It should'nt be too complicated to 
                        //extend protection logic for image level.
                        deleteHelper.DeleteData(level, identifier, false, propagateDeleteUpwards);
                        break;
                    default:
                        deleteHelper.DeleteData(level, identifier, false, propagateDeleteUpwards);
                        break;
                }
                //StorageDevicesManager.ForceCleanup(DeviceID);
            }
            finally
            {
                if (deleteToken != null)
                {
                    performanceTracer.TraceEnd(deleteToken);
                }
            }
            tracer.TraceVerbose("Exiting StorageDeviceBase.ForceDeleteData");
        }
        /// <summary>
        /// See <see cref="IDataDelete.ForceDeleteNonImageData(List{Identifier})"/>
        /// </summary>
        /// <param name="identifiers">Identifier of the non image object to be deleted</param>
        /// <returns></returns>
        public void ForceDeleteNonImageData(List<Identifier> identifiers)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.ForceDeleteNonImageData");
            PerformanceTraceToken deleteToken = null;

            try
            {
                deleteToken = performanceTracer.TraceBeginInfo(
                    deleteAction, "ForceDeleteNonImageData");
                deleteHelper.ForceDeleteNonImageData(identifiers);
            }
            finally
            {
                performanceTracer?.TraceEnd(deleteToken);
            }
            tracer.TraceVerbose("Exiting StorageDeviceBase.ForceDeleteNonImageData");
        }
        /// <summary>
        /// See <see cref="IDataDelete.DeleteProtectPatient"/>
        /// </summary>
        public string DeleteProtectPatient(StorageKey patientStorageKey)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.DeleteProtectPatient");
            if (!DatabaseStore.Configuration.SupportsDeleteProtect)
            {
                throw new NotSupportedException(
                    "The storage device does not support delete protection.");
            }

            PersistentDicomObjectCollection studies = GetStudiesUnderPatient(patientStorageKey);

            if (studies == null || studies.Count == 0)
            {
                throw new ObjectNotPresentException(
                    "There are no studies under the patient represented by the storage key : " +
                    patientStorageKey.Anonymize());
            }
            string token = Guid.NewGuid().ToString();
            List<StorageKey> deleteProtectedStudies = new List<StorageKey>();
            try
            {
                foreach (PersistentDicomObject study in studies)
                {
                    deleteProtection.DeleteProtectStudy(study.StorageKey, token);
                    deleteProtectedStudies.Add(study.StorageKey);
                }
            }
            finally
            {
                // delete protection of the patients is an atomic operation
                // Either it delete protects all the studies under the patient
                // or it delete protects nothing and throws the appropriate exception.
                if (deleteProtectedStudies.Count != studies.Count)
                {
                    foreach (StorageKey deleteProtectedStudy in deleteProtectedStudies)
                    {
                        deleteProtection.DeleteUnProtectStudy(deleteProtectedStudy, token);
                    }
                }
            }
            tracer.TraceVerbose("Exiting StorageDeviceBase.DeleteProtectPatient");
            return token;
        }

        /// <summary>
        /// See <see cref="IDataDelete.DeleteUnProtectPatient"/>
        /// </summary>
        public void DeleteUnProtectPatient(
            StorageKey patientStorageKey, string deleteProtectToken)
        {

            tracer.TraceVerbose("Entering StorageDeviceBase.DeleteUnProtectPatient");
            if (!DatabaseStore.Configuration.SupportsDeleteProtect)
            {
                throw new NotSupportedException(
                    "The storage device does not support delete protection.");
            }

            PersistentDicomObjectCollection studies = GetStudiesUnderPatient(patientStorageKey);

            if (studies == null || studies.Count == 0)
            {
                throw new ObjectNotPresentException(
                    "There are no studies under the patient represented by the storage key : " +
                    patientStorageKey.Anonymize());
            }
            foreach (PersistentDicomObject study in studies)
            {
                deleteProtection.DeleteUnProtectStudy(study.StorageKey, deleteProtectToken);
            }

            tracer.TraceVerbose("Exiting StorageDeviceBase.DeleteUnProtectPatient");
        }

        /// <summary>
        /// See <see cref="IDataDelete.IsPatientDeleteProtected"/>
        /// </summary>
        public bool IsPatientDeleteProtected(StorageKey patientStorageKey)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.IsPatientDeleteProtected");
            PersistentDicomObjectCollection studies;
            try
            {
                studies = GetStudiesUnderPatient(patientStorageKey);
            }
            catch (SqlException ex)
            {
                // sqlCon.Open() can result in either SqlException/InvalidOperationException 
                // as per documentation so handle both
                throw new ObjectNotPresentException(
                "An SQL Exception is thrown while retrieving studies under the patient " +
                "represented by the storage key : " + patientStorageKey.Anonymize(), ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new ObjectNotPresentException(
                "An SQL Exception is thrown while retrieving studies under the patient " +
                "represented by the storage key : " + patientStorageKey.Anonymize(), ex);
            }

            if (studies == null || studies.Count == 0)
            {
                throw new ObjectNotPresentException(
                    "There are no studies under the patient represented by the storage key : " +
                    patientStorageKey.Anonymize());
            }

            StorageKey anyStudyKey = studies[0].StorageKey;

            //if the device itself does not support delete protection,
            //then no patient/study stored in it can ever be delete protected.
            //Also if 1 study under the patient is delete protected then we
            //say that all the studies under the patient are delete protected.
            //This is because delete protection is only offered at the Patient Level
            //as of now. If this changes to also support delete protection at the study 
            //level then we need to revisit this logic.
            bool canDeleteAnyStudyUnderPatient =
                !DatabaseStore.Configuration.SupportsDeleteProtect ||
                !deleteProtection.IsDeleteProtected(anyStudyKey.Identifier);

            tracer.TraceVerbose("Exiting StorageDeviceBase.IsPatientDeleteProtected");
            return !canDeleteAnyStudyUnderPatient;
        }

        /// <summary>
        /// See <see cref="IDataDelete.IsDeviceDeleteProtected"/>
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public bool IsDeviceDeleteProtected(string deviceId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Protect a study from Deletion.
        /// </summary>
        /// <param name="studyKey">
        /// The study to be protected.
        /// </param>
        /// <returns>
        /// A token for the delete protection.
        /// </returns>
        public string DeleteProtectStudy(StorageKey studyKey)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.DeleteProtectStudy");
            if (!DatabaseStore.Configuration.SupportsDeleteProtect)
            {
                throw new NotSupportedException(
                    "The storage device does not support delete protection.");
            }
            string token = deleteProtection.DeleteProtectStudy(studyKey);
            tracer.TraceVerbose("Exiting StorageDeviceBase.DeleteProtectStudy");
            return token;
        }

        /// <summary>
        /// Set Study attibute to Protect the Study from Deletion.
        /// </summary>
        /// <param name="studyKey">
        /// The study to be protected.
        /// </param>
        public void LockStudy(StorageKey studyKey)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.LockStudy");
            deleteProtection.LockStudy(studyKey);
            tracer.TraceVerbose("Exiting StorageDeviceBase.LockStudy");
        }

        /// <summary>
        /// Set Study attibute to Protects/UnProtect the Study from Deletion.
        /// </summary>
        /// <param name="studyKey">
        /// The study to be protected.
        /// </param>
        public void UnlockStudy(StorageKey studyKey)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.UnlockStudy");
            deleteProtection.UnlockStudy(studyKey);
            tracer.TraceVerbose("Exiting StorageDeviceBase.UnlockStudy");
        }

        /// <summary>
        /// Checks whether the given study is delete protected.
        /// </summary>
        /// <param name="studyKey">
        /// The study to be protected.
        /// </param>
        /// <returns>
        /// True if given study is delete protected.
        /// </returns>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Catch and report any exception")]
        public bool IsStudyLocked(StorageKey studyKey)
        {
            bool isStudyDeleteProtected = false;
            tracer.TraceVerbose("Entering StorageDeviceBase.IsStudyLocked");
            try
            {
                isStudyDeleteProtected = deleteProtection.IsStudyLocked(studyKey);
            }
            catch (NullReferenceException ex)
            {
                StorageDevicesUtilities.LogError(
                    "Null reference while checking if study is locked" + ex
                    );
            }
            catch (ConstraintException ex)
            {
                StorageDevicesUtilities.LogError(
                    "Given item cannot be delted or it does not support delete protection " + ex
                    );
            }
            catch (Exception ex)
            {
                StorageDevicesUtilities.LogError(
                    "Error while checking IsStudyLocked" + ex
                    );
            }
            tracer.TraceVerbose("Exiting StorageDeviceBase.IsStudyLocked");
            return isStudyDeleteProtected;
        }

        /// <summary>
        /// Protect Series from Deletion.
        /// </summary>
        /// <param name="seriesKey">
        /// The series to be protected.
        /// </param>
        /// /// <returns>
        /// A token for the delete protection.
        /// </returns>
        public string DeleteProtectSeries(StorageKey seriesKey)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.DeleteProtectSeries");
            if (!DatabaseStore.Configuration.SupportsDeleteProtect)
            {
                throw new NotSupportedException(
                    "The storage device does not support delete protection.");
            }
            string token = deleteProtection.DeleteProtectSeries(seriesKey);
            tracer.TraceVerbose("Exiting StorageDeviceBase.DeleteProtectSeries");
            return token;
        }

        /// <summary>
        /// Release the delete protection on a study.
        /// </summary>
        /// <param name="studyKey">
        /// The study for which delete protection is to be removed.
        /// </param>
        /// <param name="deleteProtectToken">
        /// The token returned while delete protecting the study.
        /// </param>
        public void DeleteUnProtectStudy(StorageKey studyKey, string deleteProtectToken)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.DeleteUnProtectStudy");
            if (!DatabaseStore.Configuration.SupportsDeleteProtect)
            {
                throw new NotSupportedException(
                    "The storage device does not support delete protection.");
            }
            deleteProtection.DeleteUnProtectStudy(studyKey, deleteProtectToken);
            tracer.TraceVerbose("Exiting StorageDeviceBase.DeleteUnProtectStudy");
        }

        /// <summary>
        /// Release the delete protection on a series.
        /// </summary>
        /// <param name="seriesKey">
        /// The series for which delete protection is to be removed.
        /// </param>
        /// <param name="deleteProtectToken">
        /// The token returned while delete protecting the series.
        /// </param>
        public void DeleteUnprotectSeries(StorageKey seriesKey, string deleteProtectToken)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.DeleteUnprotectSeries");
            if (!DatabaseStore.Configuration.SupportsDeleteProtect)
            {
                throw new NotSupportedException(
                    "The storage device does not support delete protection.");
            }
            deleteProtection.DeleteUnprotectSeries(seriesKey, deleteProtectToken);
            tracer.TraceVerbose("Exiting StorageDeviceBase.DeleteUnprotectSeries");
        }

        /// <summary>
        /// Determines whether this instance can delete the specified identifier.
        /// </summary>
        /// <param name="identifier">The identifier to be check.</param>
        /// <returns>
        /// A boolean indicating whether the given <paramref name="identifier"/> can be deleted.
        /// </returns>
        public bool CanDelete(Identifier identifier)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.CanDelete");

            bool canDeleteSoFar = true;
            //1. check if the identifier is delete protected.
            //  if the device itself does not support delete protection,
            //  any object within that device cannot be delete protected.
            bool isDeleteProtected = DatabaseStore.Configuration.SupportsDeleteProtect &&
                deleteProtection.IsDeleteProtected(identifier);
            canDeleteSoFar &= !isDeleteProtected;

            if (canDeleteSoFar)
            {
                //2. check if the identifier has a series that is under construction.
                StorageKey key = new StorageKey(deviceId, identifier);
                bool isSeriesUnderConstructionCanBeDeleted = StoreHelper.CanUnderConstructionKeyBeDeleted(key);
                canDeleteSoFar &= isSeriesUnderConstructionCanBeDeleted;
            }

            tracer.TraceVerbose("Exiting StorageDeviceBase.CanDelete");
            return canDeleteSoFar;
        }

        /// <summary>
        /// See <see cref="IDataDelete.IsSeriesDeleteProtected"/>
        /// </summary>
        public bool IsSeriesDeleteProtected(StorageKey seriesKey)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.IsSeriesDeleteProtected");

            //if the device itself does not support delete protection,
            //then no study stored in it can ever be delete protected.
            bool canDelete =
                !DatabaseStore.Configuration.SupportsDeleteProtect ||
                !deleteProtection.IsDeleteProtected(seriesKey.Identifier);

            tracer.TraceVerbose("Exiting StorageDeviceBase.IsSeriesDeleteProtected");
            return !canDelete;
        }

        /// <summary>
        /// See <see cref="IDataDelete.IsStudyDeleteProtected"/>
        /// </summary>
        public bool IsStudyDeleteProtected(StorageKey studyKey)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.IsStudyDeleteProtected");

            //if the device itself does not support delete protection,
            //then no study stored in it can ever be delete protected.
            bool canDelete =
                !DatabaseStore.Configuration.SupportsDeleteProtect ||
                !deleteProtection.IsDeleteProtected(studyKey.Identifier);

            tracer.TraceVerbose("Exiting StorageDeviceBase.IsStudyDeleteProtected");
            return !canDelete;
        }

        #endregion

        #region IApplicationBlob

        /// <summary>
        /// See <see cref="IApplicationBlob.DoesBlobExist(Identifier, string)"/>
        /// </summary>
        public bool DoesBlobExist(Identifier identifier, string blobName)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.DoesBlobExist");
            if (identifier == null || string.IsNullOrEmpty(identifier.StudyInstanceUid))
            {
                const string errormessage =
                    "Identifier should not be null when " +
                    "checking for the existence of the blob";
                StorageDevicesUtilities.LogError(errormessage);
                throw new ArgumentNullException("identifier", errormessage);
            }
            bool doesBlobExist = applicationBlobHelper.DoesBlobExist(identifier, blobName);
            tracer.TraceVerbose("Exiting StorageDeviceBase.DoesBlobExist");
            return doesBlobExist;
        }

        /// <summary>
        /// See <see cref="IApplicationBlob.FetchBlob(Identifier, string)"/>
        /// </summary>
        public Stream FetchBlob(Identifier identifier, string blobName)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.FetchBlob(identifier, blobName)");
            if (identifier == null || string.IsNullOrEmpty(identifier.StudyInstanceUid))
            {
                const string errormessage =
                    "Identifier should not be null when " +
                    "fetching a Blob.";
                StorageDevicesUtilities.LogError(errormessage);
                throw new ArgumentNullException("identifier", errormessage);
            }
            Stream fetchBlob = applicationBlobHelper.FetchBlob(identifier, blobName);
            tracer.TraceVerbose("Exiting StorageDeviceBase.FetchBlob(identifier, blobName)");
            return fetchBlob;
        }

        /// <summary>
        /// See <see cref="IApplicationBlob.FetchBlob(string)"/>
        /// </summary>
        public Stream FetchBlob(string blobName)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.FetchBlob(identifier, blobName)");
            if (string.IsNullOrEmpty(blobName))
            {
                const string errormessage =
                    "blobName should not be null when " +
                    "fetching a Blob.";
                tracer.TraceInfo(errormessage);
                throw new ArgumentNullException("blobName", errormessage);
            }
            Stream fetchBlob = applicationBlobHelper.FetchBlob(blobName);
            tracer.TraceVerbose("Exiting StorageDeviceBase.FetchBlob(identifier, blobName)");
            return fetchBlob;
        }

        /// <summary>
        /// See <see cref="IApplicationBlob.FetchBlobs(Identifier)"/>
        /// </summary>
        public IList<Stream> FetchBlobs(Identifier identifier)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.FetchBlob(identifier)");
            if (identifier == null || string.IsNullOrEmpty(identifier.StudyInstanceUid))
            {
                throw new ArgumentNullException("identifier");
            }
            IList<Stream> fetchBlobs = applicationBlobHelper.FetchBlobs(identifier);
            tracer.TraceVerbose("Exiting StorageDeviceBase.FetchBlob(identifier)");
            return fetchBlobs;
        }

        /// <summary>
        /// See <see cref="IApplicationBlob.StoreBlob(Identifier, string, Stream)"/>
        /// </summary>
        public void StoreBlob(Identifier identifier, string blobName, Stream blobStream)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.StoreBlob");
            if (identifier == null || string.IsNullOrEmpty(identifier.StudyInstanceUid))
            {
                const string errormessage = "identifier should not be null while Storing a Blob";
                StorageDevicesUtilities.LogError(errormessage);
                throw new ArgumentNullException("identifier", errormessage);
            }
            applicationBlobHelper.StoreBlob(identifier, blobName, blobStream);
            tracer.TraceVerbose("Exiting StorageDeviceBase.StoreBlob");
        }

        /// <summary>
        /// See <see cref="IApplicationBlob.StoreBlob(string, Stream)"/>
        /// </summary>
        public void StoreBlob(string blobName, Stream blobStream)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.StoreBlob");
            if (string.IsNullOrEmpty(blobName))
            {
                const string errormessage = "Blob Name should not be null while Storing a Blob";
                tracer.TraceInfo(errormessage);
                throw new ArgumentNullException("blobName", errormessage);
            }
            applicationBlobHelper.StoreBlob(blobName, blobStream);
            tracer.TraceVerbose("Exiting StorageDeviceBase.StoreBlob");
        }

        /// <summary>
        /// See <see cref="IApplicationBlob.DeleteBlob(Identifier, string)"/>
        /// </summary>
        public void DeleteBlob(Identifier identifier, string blobName)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.DeleteBlob");
            if (identifier == null || string.IsNullOrEmpty(identifier.StudyInstanceUid))
            {
                const string errormessage = "identifier should not be null while Deleting a Blob";
                StorageDevicesUtilities.LogError(errormessage);
                throw new ArgumentNullException("identifier", errormessage);
            }
            applicationBlobHelper.DeleteBlob(identifier, blobName);
            //StorageDevicesManager.ForceCleanup(DeviceID, true);
            tracer.TraceVerbose("Exiting StorageDeviceBase.DeleteBlob");
        }

        /// <summary>
        /// See <see cref="IApplicationBlob.DeleteBlob(string)"/>
        /// </summary>
        public void DeleteBlob(string blobName)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.DeleteBlob");
            if (string.IsNullOrEmpty(blobName))
            {
                const string errormessage = "blobName should not be null while Deleting a Blob";
                StorageDevicesUtilities.LogError(errormessage);
                throw new ArgumentNullException("blobName", errormessage);
            }
            applicationBlobHelper.DeleteBlob(blobName);
            //StorageDevicesManager.ForceCleanup(DeviceID, true);
            tracer.TraceVerbose("Exiting StorageDeviceBase.DeleteBlob");
        }

        /// <summary>
        /// See <see cref="IApplicationBlob.DoesBlobExist(string)"/>
        /// </summary>
        public bool DoesBlobExist(string blobName)
        {
            tracer.TraceVerbose("Entering DatabaseService.DoesBlobExist");
            IApplicationBlob storageDevice =
                StorageDeviceProvider.GetDevice(deviceId) as IApplicationBlob;
            if (blobName == null)
            {
                const string errormessage = "Given blobName does not support blob storage.";
                StorageDevicesUtilities.LogError(errormessage);
                throw new ArgumentNullException("blobName", errormessage);
            }
            tracer.TraceVerbose("Exiting DatabaseService.DoesBlobExist");
            return storageDevice != null &&
                applicationBlobHelper.DoesBlobExist(blobName);
        }

        #endregion

        #region QueryAsync methods
        /// <summary>
        /// This method calls the actual Query call in the IBasicStorageDevice.
        /// </summary>
        /// <param name="level">level</param>
        /// <param name="parentIdentifier">parentIdentifier</param>
        /// <param name="queryFilter">filter</param>
        /// <param name="state">represents unique identifier for the asycn call.</param>
        [
        SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "MSBuild tasks should fail gracefully")
        ]
        private void AsynchronousQuery(
            QueryLevel level,
            Identifier parentIdentifier,
            QueryFilter queryFilter,
            object state
        )
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.AsynchronousQuery");
            bool isCancelled;
            DeviceQueryCompletedEventArgs completedArgs = null;
            QueryResult queryResult = null;

            lock (stateToIsCancelled)
            {
                stateToIsCancelled.TryGetValue(state, out isCancelled);
            }

            try
            {
                if (!isCancelled)
                {
                    queryResult = queryHelper.QueryData(
                        level,
                        parentIdentifier,
                        queryFilter
                    );

                    //When the QueryProgress event is fired, the queryResult will be extracted
                    //as a PDO collection. While doing this, the query result stream will be closed
                    //So, we have to send another copy of the stream so that the queryComplete
                    //can have the actual results.
                    QueryResult copyOfQueryResult;
                    using (var memoryStream = new RecyclableBufferMemoryStream())
                    {
                        if (
                            queryResult.QueryResultList != null &&
                            queryResult.QueryResultList.Count > 0
                        )
                        {
                            copyOfQueryResult =
                                new QueryResult(queryResult.QueryResultList);
                        }
                        else
                        {
                            queryResult.QueryResultStream.CopyTo(memoryStream);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            queryResult.QueryResultStream.Seek(0, SeekOrigin.Begin);
                            copyOfQueryResult = new QueryResult(memoryStream);
                        }
                    }

                    DeviceQueryProgressEventArgs progressArgs =
                        new DeviceQueryProgressEventArgs(
                            DeviceID,
                            level,
                            copyOfQueryResult,
                            100,
                            state);
                    OnQueryProgressChanged(progressArgs);
                }

                completedArgs =
                    new DeviceQueryCompletedEventArgs(
                        null,
                        false,
                        state,
                        DeviceID,
                        level,
                        queryResult);
            }
            catch (Exception ex)
            {
                StorageDevicesUtilities.LogError(
                    "Error in Asynchronous Query: QueryLevel = " + level.ToString("G") +
                    " Parent ID = " + parentIdentifier.Anonymize() + " IsCancelled = " +
                    isCancelled, ex);
                completedArgs =
                    new DeviceQueryCompletedEventArgs(
                        ex,
                        false,
                        state,
                        DeviceID,
                        level,
                        queryResult);
            }
            finally
            {
                OnQueryCompleted(completedArgs);
            }
            tracer.TraceVerbose("Exiting StorageDeviceBase.AsynchronousQuery");
        }

        ///// <summary>
        ///// Callback method which gets executed at the end of asynchronousquery.
        ///// </summary>
        ///// <remarks>
        ///// Gets the delegate and does an endinvoke.
        ///// </remarks>
        ///// <param name="asyncresult">asyncResult</param>
        //private void AsynchronousQueryCallback(IAsyncResult asyncresult) {
        //    AsyncResult result = (AsyncResult)asyncresult;
        //    QueryAsyncDelegate caller = (QueryAsyncDelegate)result.AsyncDelegate;
        //    caller.EndInvoke(asyncresult);
        //}

        /// <summary>
        /// This method raises a Progress Changed Event.
        /// </summary>
        /// <param name="args"></param>
        private void OnQueryProgressChanged(DeviceQueryProgressEventArgs args)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.OnQueryProgressChanged");
            EventHandler<DeviceQueryProgressEventArgs> progressChanged = AIPQueryProgressChanged;
            if (progressChanged != null)
            {
                foreach (
                    EventHandler<DeviceQueryProgressEventArgs> handler in
                        progressChanged.GetInvocationList()
                )
                {
                    handler?.BeginInvoke(this, args, null, null);
                }
            }
            tracer.TraceVerbose("Exiting StorageDeviceBase.OnQueryProgressChanged");
        }

        /// <summary>
        /// This method raises a Completed event mentioning completion of Async Query.
        /// </summary>
        /// <remarks>
        /// In case of Cancellation, cancel bool should be set in the args and this method should
        /// be called.
        /// </remarks>
        /// <param name="args">completed arguments</param>
        private void OnQueryCompleted(DeviceQueryCompletedEventArgs args)
        {
            tracer.TraceVerbose("Entering StorageDeviceBase.OnQueryCompleted");
            EventHandler<DeviceQueryCompletedEventArgs> completed = AIPQueryCompleted;
            lock (stateToIsCancelled)
            {
                if (stateToIsCancelled.ContainsKey(args.UserState))
                {
                    stateToIsCancelled.Remove(args.UserState);
                }
            }
            if (completed != null)
            {
                foreach (
                    EventHandler<DeviceQueryCompletedEventArgs> handler in
                        completed.GetInvocationList()
                )
                {
                    if (handler != null)
                    {
                        handler.BeginInvoke(this, args, null, null);
                    }
                }
            }
            tracer.TraceVerbose("Exiting StorageDeviceBase.OnQueryCompleted");
        }
        #endregion

        #region private methods

        //Gets the studies under the patient storage key.
        private PersistentDicomObjectCollection GetStudiesUnderPatient(
            StorageKey patientStorageKey)
        {

            if (patientStorageKey.Identifier.PatientKey == null)
            {
                throw new ArgumentException("The patientKey associated with the storage key : " +
                    patientStorageKey.Anonymize() + " is not valid.");
            }
            QueryResult studyResults = QueryData(
            QueryLevel.Study,
            patientStorageKey.Identifier,
            QueryFilter.MatchAll());
            return StorageDevicesUtilities.ExtractResultsAsPDO(
            deviceId,
            QueryLevel.Study,
            studyResults);
        }

        #endregion

        #region IDataStream

        /// <summary>
        /// Fetches the information through stream about the identifiers
        /// </summary>
        /// <param name="identifiers">The list of identifiers</param>
        /// <param name="mode">The data loading mode</param>
        /// <param name="skipLoadingFrames">Skip loading frame data</param>
        /// <returns>
        /// The list of <see cref="FetchInformation"/> containing image information at series level
        /// </returns>
        public IList<FetchInformation> FetchAsStream(
            IList<Identifier> identifiers,
            DataLoadingMode mode,
            bool skipLoadingFrames
        )
        {
            return fetchHelper.FetchAsStream(identifiers, mode, skipLoadingFrames);
        }

        #endregion

    }
}
