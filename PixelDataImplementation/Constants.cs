namespace PixelDataImplementation
{
    /// <summary>
    /// Configuration constants
    /// </summary>
    internal static class Constants
    {
        internal const string GenericObjectAccessBase = "GenericObjectAccessBase";
        internal const string ImagingStudyAccessBase = "ImagingStudyAccessBase";
        internal const string DicomObjectAccessBase = "DicomObjectAccessBase";
        internal const string ConfigurationBase = "ConfigurationBase";
        internal const string AssemblyPath = "AssemblyPath";
        internal const string BulkFolder = "BulkFolder";
        internal const string Scheme = "Scheme";
        internal const string DicomServiceName = "DicomServiceName";

        internal const string GlobalSettings = "GlobalSettings";
        internal const string HomeDirectory = "HomeDirectory";
        internal const string UserName = "UserName";
        internal const string Password = "Password";
        internal const string DeviceType = "DeviceType";
        internal const string SQLServer = "SqlServer";
        internal const string SQLite = "SQLite";
        internal const string FileRepository = "FileRepository";
        internal const string ProxyDevice = "ProxyDevice";
        internal const string ProxyDeviceFolderName = "ProxyDevices";
        internal const string TempStorage = "TemporaryStorageDevice";
        internal const string StagingAreaFolder = "StagingAreaFolder";
        internal const string SqlServerConnectionString = "SqlServerConnectionString";
        internal const string DatabaseQuota = "DatabaseQuota";
        internal const string RemoteSettings = "RemoteSettings";
        internal const string Enabled = "Enabled";
        internal const string DevicesAssociatedWithPrimary =
            "DevicesAssociatedWithPrimary";
        internal const string ObjOidColumn = "objOid";
        internal const string UidColumn = "uid";
        internal const string ParOidColumn = "parOid";
        internal const string ParUidColumn = "parUid";
        internal const string FileStorePathColumn = "fileStorePath";
        internal const string ReconciliationStatus = "reconciliationStatus";
        internal const string PreviousPatientId = "previousPatientId";
        internal const string PreviousPatientName = "previousPatientName";
        internal const string StorablesColumn = "storables";
        internal const string CompletedColumn = "completed";
        internal const string DeviceIdColumn = "deviceId";
        internal const string IsHidden = "IsHidden";
        internal const string HumanReadableName = "HumanReadableName";
        internal const string DatabasePath = "DatabasePath";

        internal const string SqlDatabaseName = "SqlDatabaseName";
        internal const string SqlDeviceFolderName = "SqlDeviceFolderName";
        internal const string PixelDataPath = "PixelDataPath";
        internal const string DicomDirFileDevicePath = "DicomDirFileDevicePath";
        internal const string DicomDirFileNameTag = "DicomDirFileName";
        internal const string DicomDirFileName = "DICOMDIR";
        internal const string DicomDirFolderName = "DICOM";
        internal const string PrimaryDatabase = "PrimaryDatabase";
        internal const string IsReadOnly = "IsReadOnly";
        internal const string IsPathDynamic = "IsPathDynamic";
        internal const string HomeDir = "%DATABASE_HOME%";
        internal const string DeviceDefinitions = "DeviceDefinitions";
        internal const string DeviceId = "DeviceId";
        internal const string SectionFormat = "[{0}:{1}]";
        internal const string Compressed = "Compressed";

        internal const string DeleteProtectFilePrefix = "DeleteProtect_";

        internal const string DeviceSpeed = "DeviceSpeed";

        internal const string IsArchive = "IsArchive";

        internal const string SupportsDeleteProtect = "SupportsDeleteProtect";
        internal const string DeleteProtectTimeoutInHours = "DeleteProtectTimeoutInHours";

        internal const string SeriesUnderConstructionTimeOutInMinutes =
            "SeriesUnderConstructionTimeOutInMinutes";

        internal const string FastAccessCacheSize = "FastAccessCacheSize";
        internal const string IsAuditRequired = "IsAuditRequired";

        public const string ServiceUrl = "ServiceUrl";
        public const string UseWcf = "UseWcf";
        public const string ServiceName = "ServiceName";

        internal const string SkipRebuild = "SkipRebuild";
        internal const string DefaultFastAccessCacheSize = "DefaultFastAccessCacheSize";
        internal const string DelayStoreBySeconds = "DelayStoreBySeconds";

        internal const int DefaultNumberOfSeriesToCache = 50;
        internal const int DefaultDelayStoreBySeconds = 30;

        internal const string CentralizedDatabaseSettings = "CentralizedDatabaseSettings";
        internal const string UseCentralizedDB = "UseCentralizedDB";

        internal const string FileNumberFormat = "0000";

        internal const int MaxAllowedFileCount = 2048;

        internal const string ImageFileNamePrefix = "IM_";

        internal const string PsFileNamePrefix = "PS_";

        internal const string SrFileNamePrefix = "SR_";

        internal const string KsFileNamePrefix = "KS_";

        internal const string ForiegnFileNamePrefix = "FF_";

        internal const string PrivateFileNamePrefix = "XX_";

        internal const int DefaultDatabaseQuotaInMB = 500;
        internal const int DefaultDeleteProtectTimeoutInHours = 3;
        internal const int DefaultSeriesUnderConstructionTimeoutInMinutes = 60;

        internal const string DBAsRemoteDicomServer = "DBAsRemoteDicomServer";
        internal const string UseDBAsRemoteDicomServer = "UseDBAsRemoteDicomServer";
        internal const string FileStorePath = "FileStorePath";

        internal const string AutoDeleteConfiguration = "AutoDeleteConfiguration";
        internal const string EnableAutoDeleteTask = "EnableAutoDeleteTask";
        internal const string HighThresholdDiskSpace = "HighThresholdDiskSpace";
        internal const string LowThresholdDiskSpace = "LowThresholdDiskSpace";
        internal const string DiskSpaceThresholdLevel = "DiskSpaceThresholdLevel";
        internal const ulong DefaultDiskSpaceThresholdLevel = 500;
        internal const int DiskFreeSpaceCheckIntervalInSeconds = 1;

        internal const string DefaultLockMode = "DefaultLockMode";
        internal const string LockMode = "LockMode";

        internal const string GrantAccessToUserGroup = "GrantAccessToUserGroup";

        internal const string EnableFlushToDisk = "EnableFlushToDisk";
        internal const string EnableMultiplePpsPerStudy = "EnableMultiplePpsPerStudy";

        internal const string EnableFlushFileBuffers = "EnableFlushFileBuffers";

        internal const string EnableStorageRecordCache = "EnableStorageRecordCache";
        internal const string StorageRecordCacheSize = "StorageRecordCacheSize";

        internal const int DefaultStorageRecordCacheSize = 3; // Default is 3 (configurable)

        internal const string EnableFullHeaderCache = "EnableFullHeaderCache";

        internal const string CacheExpirationTime = "CacheExpirationTime";

        internal const int DefaultCacheExpirationTime = -1; // Default is -1 (means no expiration)

        internal const string BulkDataReferenceThresholdSize = "BulkDataReferenceThresholdSize";
        //Default Bulk data reference threshold size is 5MB
        internal const int DefaultBulkDataReferceThreasholdSize = 5242880;

        internal const string NumberOfThreadsForRoboCopy = "NumberOfThreadsForRoboCopy";
        //Maximum Number Of Threads For RoboCopy is 128
        internal const int MaximumNumberOfThreadsForRoboCopy = 128;

        internal const string Unknown = "UNKNOWN";
        internal const string Completed = "Completed";
        internal const string AcqCompleted = "AcqCompleted";
        internal const string DatabaseService = "DatabaseService";

        internal const string DataCorruptionException = "DataCorruptionException";
        internal const string DeviceAccessException = "DeviceAccessException";
        internal const string ArgumentException = "ArgumentException";
        internal const string ArgumentNullException = "ArgumentNullException";
        internal const string DeviceConfigurationException = "DeviceConfigurationException";
        internal const string FailException = "FailException";

        internal const string IsStorageDeviceRemote = "IsStorageDeviceRemote";

        internal const string IsDBCacheSyncServiceEnabled = "IsDBCacheSyncServiceEnabled";

        internal const string SidsAttributes = "SIDSAttributes";
        internal const string ConstantSopClassUids = "ConstantSopClassUIDs";
        internal const string AutoDeleteSopClassUIDs = "AutoDeleteSopClassUIDs";
        internal const string QueryableAttrs = "QueryableAttrs";
        internal const string FastAccessAttrs = "FastAccessAttrs";
        internal const string IndexAttrs = "IndexAttrs";
        internal const string SkipStateAttrs = "SkipUpdateAttributes";
        internal const string DicomDir = "DicomDir";
        internal const string Patient = "Patient";
        internal const string Study = "Study";
        internal const string Series = "Series";
        internal const string Image = "Image";
        internal const string Report = "Report";
        internal const string Status = "Status";
        internal const string SeriesModificationCompleted = "ModificationCompleted";
        internal const string SeriesUnderModification = "UnderModification";
        internal const string EnablePatientReconciliation = "EnablePatientReconciliation";
        internal const string NumberOfConcurrentReaders = "NumberOfConcurrentReaders";
        internal const int MinimumNumberOfConcurrentReaders = 10;
        internal const string RisQueryableAttributes = "RisQueryableAttributes";
        internal const string IncludeAttributesInRisResponse = "IncludeAttributesInRisResponse";
        //Constants related to SeriesStateAggregatorConfig file
        internal const string SeriesCategory = "SeriesCategory";
        internal const string TagName = "TagName";
        internal const string TagValue = "TagValue";
        internal const string IsAggregationRequired = "IsAggregationRequired";
        /// <summary>
        /// Root Element of SIDS
        /// </summary>
        internal const string SIDSRootElement = "SIDS";

        /// <summary>
        /// Root Element of SIDS MedicalRoot formate
        /// </summary>
        internal const string SIDSMedicalRootElement = "MedicalSystem";

        /// <summary>
        /// Version Attribute of SIDS Root node
        /// </summary>
        internal const string Version = "Version";

        /// <summary>
        /// Environment variable name which hold SIDS path
        /// </summary>
        internal const string SIDSEnvironmentVar = "SidsFilePath";

        /// <summary>
        /// SIDS CDF 2.0.0 Version
        /// </summary>
        internal const string SIDSPath = @"SIDS\SIDS.xml";

        /// <summary>
        /// Custom Action id for DeleteNonDicomImage
        /// </summary>
        internal const string DeleteNonDicomImage = "$DELETENONDICOMIMAGE";

        /// <summary>
        /// Custom Action id for SetPrintSeriesState
        /// </summary>
        internal const string SetPrintSeriesState = "PRINTED";

        /// <summary>
        /// Custom Action id for SetMediaSeriesState
        /// </summary>
        internal const string SetMediaSeriesState = "EXPORTED";

        /// <summary>
        /// Custom Action id for Archived workflow state
        /// </summary>
        internal const string ArchivedWorkflowState = "ARCHIVED";

        /// <summary>
        /// Represents the series state when No instance under it are Printed / Exported
        /// </summary>
        internal const string SeriesStateInitial = "INITIAL";

        /// <summary>
        /// Represents the series state when all the instances under the series are not 
        /// printed/exported i.e instances Printed/Exported is less than 
        /// the total instances under the series
        /// </summary>
        internal const string SeriesStatePartial = "PARTIAL";

        /// <summary>
        /// Represents the series state when All instance under it are Printed/Exported
        /// </summary>
        internal const string SeriesStateComplete = "COMPLETED";

        /// <summary>
        /// Represents the series state when series export is failed
        /// </summary>
        internal const string SeriesStateFailed = "FAILED";

        /// <summary>
        /// Represents the series state when export starts
        /// </summary>
        internal const string StatePending = "PENDING";

        /// <summary>
        /// Represents the state for a PR series when export starts
        /// </summary>
        internal const string StateSkipped = "SKIPPED";

        /// <summary>
        ///  Represents the series to be transfered or not
        /// </summary>
        internal const string PiimDoNotTransfer = "DONOTTRANSFER";

        /// <summary>
        /// Represents the study state when acquisition is in progress
        /// </summary>
        internal const string StateInAcquisition = "INACQUISITION";

        /// <summary>
        /// Represents the study state when study import Completed
        /// </summary>
        internal const string ImportStudyStateComplete = "COMPLETED";

        /// <summary>
        /// Represents the study state when import is Canceled
        /// </summary>
        internal const string ImportStudyStateCanceled = "CANCELED";

        /// <summary>
        /// Represents the study state when study import Fails
        /// </summary>
        internal const string ImportStudyStateFailed = "FAILED";

        /// <summary>
        /// Represents the study state when study import Starts
        /// </summary>
        internal const string ImportStudyStateInProgress = "INPROGRESS";

        /// <summary>
        /// PSC install Path 
        ///  </summary>
        internal const string ConfPath = @"C:\Program Files (x86)\PSC4.2\ServiceConfiguration";
        internal const string PatientUniquenessTags = "PatientUniquenessTags";

        internal const string PatientIdTag = "PATIENTID";
        internal const string PatientLastNameTag = "PATIENTLASTNAME";
        internal const string PatientFirstNameTag = "PATIENTFIRSTNAME";

        internal const char MultiFileDelimeter = ';';

        internal const string AuthorizedGroups = "AuthorizedGroups";

        internal const string AuthorizedUsers = "AuthorizedUsers";

        internal const string CertificateThumbprint = "CertificateThumbprint";

        internal const string SecureCommunicationEnabled = "EnableSecurity";

        internal const string ServiceSecurity = "ServiceSecurity";

        internal const string Separator = "#";

        internal const int DefaultSecurityPort = 443;

        internal const string IgnoreInProgressRecords = "IgnoreInProgressRecords";
        internal const string StoreSeriesFullHeaderMetadataAsJson =
            "StoreSeriesFullHeaderMetadataAsJson";

        internal const string PrimaryKey = "PrimaryKey";
        internal const string ForeignKey = "ForeignKey";
        internal const string PpsId = "PpsId";
        internal const string StudyInstanceId = "StudyInstanceId";
        internal const string MppsFilePath = "MppsFilePath";
        internal const string PpsFilePrefix = "PPS_";
        internal const string SpsFilePrefix = "SPS_BLOB";
        internal const string Mpps = "Mpps";


        internal const int ErrorDiskFull = 112;
        internal const int ErrorWriteProtect = 19;
        internal const int ErrorFileNotFound = 2;
        internal const int ErrorPathNotFound = 3;


        //DataServerCaching service configuration
        internal const string DataServerCachingService = "DataServerCachingService";
        ///DataServerCaching service URI.
        internal const string DataServerCachingServiceUri =
            "net.pipe://localhost/DataServerCachingService";
        internal const string DataServerCachingServiceMutexName =
            "Global\\{{{DataServerCachingService_SingleInstance_Mutex}}}";

        internal const string MaxCacheLifetime = "MaxCacheLifetime";
        internal const string MaxFastHeaderSeriesCount = "MaxFastHeaderSeriesCount";
        internal const string MaxFullHeaderSeriesCount = "MaxFullHeaderSeriesCount";
        internal const int MaxSeriesCount = 50;
        internal const int MaxCacheLifetimeValue = 10;
        internal const string IsWriteOnly = "IsWriteOnly";
        internal const string Tags = "Tags";

    }
}