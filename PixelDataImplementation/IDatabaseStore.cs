using System.Collections.Generic;
using System.IO;

using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;
using Philips.Platform.Common.DataAccess;
using Philips.Platform.StorageDevices.Reconciliation;
using Philips.Platform.StorageDevicesClient;

using DicomObject = Philips.Platform.Dicom.Data.DicomObject;
using AIIDicomObject = Philips.Platform.Common.DicomObject;

namespace PixelDataImplementation
{
    /// <summary>
    /// Basic database store methods. 
    /// Supporting Connection, Tables creation and data population
    /// </summary>
    internal interface IDatabaseStore
    {

        /// <summary>
        /// Gets or Sets the device id
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// Returns true if database exists.
        /// </summary>
        /// <returns></returns>
        /// <param name="curLocation">if Database exists curLocation points to
        /// full path of the database.</param>
        bool IsDatabasePresent(out string curLocation);

        /// <summary>
        /// Returns if the connection string is valid
        /// </summary>
        /// <returns>true if the connection string is valid, false otherwise</returns>
        bool IsConnectionStringValid();

        /// <summary>
        /// Checks the schema of currently installed Database
        /// </summary>
        /// <returns>
        /// True if Schema is valid, false otherwise
        /// </returns>
        bool IsValidDatabaseSchema(Schema schema);

        /// <summary>
        /// Update Schema of database
        /// </summary>
        /// <returns>
        /// DB Schema changes 
        /// </returns>
        void UpdateSchema(DBSchemaChanges schemaChanges);

        /// <summary>
        /// Creates database as specified in the configuration.
        /// </summary>
        void CreateDatabase(bool reCreate);

        /// <summary>
        /// Drops the database
        /// </summary>
        void DropDatabase();

        /// <summary>
        /// Connects to the database
        /// </summary>
        void Connect();

        /// <summary>
        /// Closes the database connection
        /// </summary>
        void Close();

        /// <summary>
        /// Creates the database tables based on the given schema
        /// </summary>
        /// <param name="schema">the database schema</param>
        void CreateTables(Schema schema);

        /// <summary>
        /// Creates the database tables based on the given schema
        /// </summary>
        /// <param name="schema">Scheduled workflow database schema</param>
        void CreateTables(ScheduledWorkflowDatabaseSchema schema);

        /// <summary>
        /// Delete table
        /// </summary>
        /// <param name="tableName">name of the table to be deleted</param>
        void DeleteTable(string tableName);

        /// <summary>
        /// Inserts a new row in the database based on given parameters
        /// </summary>
        /// <param name="rowRecord">data to be inserted in a row</param>
        /// <param name="objectOid">object oid of the inserted row</param>
        void InsertRow(RowRecord rowRecord, out long objectOid);

        /// <summary>
        /// Checks the database table at given level for given keys 
        /// </summary>
        /// <param name="level">the normalized database level</param>
        /// <param name="instanceUid">unique key value to be checked</param>
        /// <param name="parentInstanceUid">parent key value to be checked</param>
        /// <returns>true if the row exists, false otherwise</returns>
        bool RowExists(Level level, string instanceUid, string parentInstanceUid);

        /// <summary>
        /// Queries DICOM data from database based on the filter.
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
        /// <param name="onlyCompleted"> 
        /// Determine whether to return only fully committed data or all.
        /// </param>
        /// <returns>
        /// A list of <see cref="PersistentDicomObject"/>. 
        /// <see cref="PersistentDicomObject"/> contains a <see cref="StorageKey"/> and
        /// <see cref="DicomObject"/>.
        /// </returns> 
        Stream QueryData(
            Level level,
            Identifier parentIdentifier,
            QueryFilter filters,
            DictionaryTagsCollection sortCriteria = null,
            QuerySortOrder sortOrder = QuerySortOrder.None,
            int maxRecords = 200,
            bool onlyCompleted = false);

        /// <summary>
        /// Gets the number of matching records corresponding to the given query criteria.
        /// </summary>
        /// <param name="level">
        /// The level to match.
        /// </param>
        /// <param name="parentIdentifier">
        /// The parent identifier under which to check. For parent is null, then the query is
        /// at study level.
        /// </param>
        /// <param name="filters">
        /// The filters to match for.
        /// </param>
        /// <returns>
        /// The number of records which match the query criteria specified.
        /// </returns>
        int GetNumberOfMatchingRecords(
            Level level,
            Identifier parentIdentifier,
            QueryFilter filters);

        /// <summary>
        /// Deletes the row for the given key and reports how many children are there
        /// for the parent.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="objectOid"></param>
        /// <param name="parentOid"></param>
        /// <param name="throwException"></param>
        /// <returns></returns>
        int DeleteRow(
            Level level,
            long objectOid,
            out long parentOid,
            bool throwException);

        /// <summary>
        /// Deletes the specified row.
        /// Primary key based.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="instanceUid"></param>
        /// <param name="parentInstanceUid"></param>
        /// <param name="parentOid"></param>
        /// <param name="throwException"></param>
        /// <returns></returns>
        int DeleteRow(
            Level level,
            string instanceUid,
            string parentInstanceUid,
            out long parentOid,
            bool throwException);

        /// <summary>
        /// Delete all rows from all tables (Patient/Study/Series) 
        /// </summary>
        int DeleteAllRows();

        /// <summary>
        /// Gets the database Configuration
        /// </summary>
        DeviceConfiguration Configuration { get; }

        /// <summary>
        /// Gets the parent uid for a given key.
        /// Applicable for Series and Study level.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="instanceUid"></param>
        /// <param name="parentInstanceUid"></param>
        /// <returns></returns>
        string GetParent(
            Level level,
            string instanceUid,
            string parentInstanceUid = "");

        /// <summary>
        /// Gets the object oid for a given key.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="instanceUid"></param>
        /// <param name="parentInstanceUid"></param>
        /// <returns></returns>
        long GetObjectOid(
            Level level,
            string instanceUid,
            string parentInstanceUid = "");

        /// <summary>
        /// Updates the row with the given delta dicom object.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="instanceUid">The instance uid.</param>
        /// <param name="parentInstanceUid">The parent instance uid.</param>
        /// <param name="delta">The delta.</param>
        /// <param name="rowContent">The path</param>
        /// <param name="objectOid">The object oid of the updated row.</param>
        void UpdateRow(
            Level level,
            string instanceUid,
            string parentInstanceUid,
            DicomObject delta,
            RowContent rowContent,
            out long objectOid);

        /// <summary>
        /// Updates the row with the given delta dicom object.
        /// </summary>
        /// <param name="TxnHandler"></param>
        /// <param name="deviceId"></param>
        /// <param name="level">The level.</param>
        /// <param name="instanceUid">The instance uid.</param>
        /// <param name="patientUid">The parentUid.</param>
        /// <param name="parentUid"></param>
        /// <param name="delta">The delta.</param>
        /// <param name="rowContent">The path</param>
        /// <param name="reconciliationStatusFlag">whether reconciled or not</param>
        void UpdateRowInTxn(
            TransactionHandler TxnHandler,
            string deviceId,
            Level level,
            string instanceUid,
            string patientUid,
            string parentUid,
            DicomObject delta,
            RowContent rowContent,
            ReconciliationStatusFlag reconciliationStatusFlag);

        /// <summary>
        /// Gets the fast access dicom object
        /// </summary>
        /// <param name="level"></param>
        /// <param name="instanceUid"></param>
        /// <param name="parentInstanceUid"></param>
        /// <returns></returns>
        DicomObject GetStorables(
            Level level,
            string instanceUid,
            string parentInstanceUid = "");

        /// <summary>
        /// Gets the fast access dicom object
        /// </summary>
        /// <param name="Txn"></param>
        /// <param name="level"></param>
        /// <param name="instanceUid"></param>
        /// <param name="parentInstanceUid"></param>
        /// <returns></returns>
        DicomObject GetStorables(
            TransactionHandler Txn,
            Level level,
            string instanceUid,
            string parentInstanceUid = "");

        /// <summary>
        /// Gets the path for stored study and series from Database
        /// </summary>
        /// <param name="Txn"></param>
        /// <param name="uid"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        DicomObject GetStudyStorable(TransactionHandler Txn, string uid, string parent);

        /// <summary>
        /// Gets path for stored study from Database
        /// </summary>
        /// <param name="level"></param>
        /// <param name="instanceUid"></param>
        /// <param name="parentInstanceUid"></param>
        /// <returns></returns>
        string GetPathFromDatabase(
            Level level,
            string instanceUid,
            string parentInstanceUid = "");

        /// <summary>
        /// Gets all the studies which has original study Instance UID 
        /// same as <paramref name="studyInstanceUID"/>.
        /// Returns (study parentKey,  study instance UID, storables, study path,
        /// reconciliation status).
        /// </summary>
        /// <param name="studyInstanceUID"></param>
        /// <returns>
        /// A list of tuples, where each tuple is a 
        /// set of (study parentKey,  study instance UID, original study uid, 
        /// storables,studyPath, reconciliation status)
        /// </returns>
        List<System.Tuple<string, string, string, byte[], string, int, string[]>>
            GetMatchingStudies(
            string studyInstanceUID);

        /// <summary>
        /// Updates the column with the given value by dictionary tag.
        /// </summary>
        /// <param name="level">Study\Series</param>
        /// <param name="instanceUid">Instance UID</param>
        /// <param name="dictionaryTag">Dictionary Tag</param>
        /// <param name="value">Value</param>
        /// <param name="parentInstanceUid">Parent instance UID</param>
        void UpdateColumnByDictionaryTag(
            Level level,
            string instanceUid,
            DictionaryTag dictionaryTag,
            object value,
            string parentInstanceUid = null);

        /// <summary>
        /// Updates the column with the given column Value.
        /// </summary>
        /// <param name="level">Study\Series</param>
        /// <param name="instanceUid">Instance UID</param>
        /// <param name="columnName">columnName</param>
        /// <param name="value">value</param>
        /// <param name="parentInstanceUid">Parent instance UID</param>
        void UpdateColumn(
            Level level,
            string instanceUid,
            string columnName,
            object value,
            string parentInstanceUid = null);

        /// <summary>
        /// Updates the column with the given column Value.
        /// </summary>
        /// <param name="txnHandler"></param>
        /// <param name="deviceId"></param>
        /// <param name="level">Study\Series</param>
        /// <param name="instanceUid">Instance UID</param>
        /// <param name="columnName">columnName</param>
        /// <param name="value">value</param>
        /// <param name="parentInstanceUid">Parent instance UID</param>
        void UpdateColumnInTxn(TransactionHandler txnHandler,
            string deviceId,
            Level level,
            string instanceUid,
            string columnName,
            string value,
            string parentInstanceUid = null);

        /// <summary>
        /// Gets all study store paths under device.
        /// </summary>
        Dictionary<long, string> GetAllStudyStorePaths();

        /// <summary>
        /// Gets series instanceUid and file store path under a given study
        /// </summary>
        List<KeyValuePair<string, string>> GetSeriesIdAndFileStorePathUnderGivenStudy(
            string deviceId, string studyInstanceUid);

        /// <summary>
        /// Delete series using relative series file store path. 
        /// Also deletes study and patient data,
        /// if no series under study and no study under patient found.
        /// </summary>
        /// <param name="fileStorePath">relative path</param>
        void DeleteSeriesDbContent(string fileStorePath);

        /// <summary>
        /// Gets the instace uids.
        /// </summary>
        /// <param name="level">Study\Series</param>
        /// <param name="parentIdentifier">Parent identifer</param>
        /// <param name="filter">The filter.</param>
        /// <returns>List of instance uids</returns>
        List<string> GetInstanceUids(Level level, Identifier parentIdentifier, QueryFilter filter);

        /// <summary>
        /// Inserts a new record into database table <paramref name="tableName"/> 
        /// based on given parameters
        /// </summary>
        /// <param name="transactionHandler">Transaction handler</param>
        /// <param name="tableName">Name of the table to insert record</param>
        /// <param name="values">values to be inserted</param>
        /// <param name="dicomObject">value to populate queryable attributes</param>
        void InsertRecord(
            TransactionHandler transactionHandler,
            string tableName,
            Dictionary<string, object> values,
            DicomObject dicomObject
        );

        /// <summary>
        /// Get Mpps details
        /// </summary>
        /// <param name="studyId">Study instance Id</param>
        /// <returns><see cref="IList{DicomObject}"/></returns>
        ICollection<DicomObject> GetPerformedProcedureSteps(string studyId);

        /// <summary>
        /// Get Mpps details
        /// </summary>
        /// <param name="ppsId">Study instance Id</param>
        /// <returns><see cref="DicomObject"/></returns>
        DicomObject GetPerformedProcedureStep(string ppsId);

        /// <summary>
        /// Replace Pps
        /// </summary>
        /// <param name="performedProcedureStep">Mpps object to be replaced</param>
        /// <param name="ppsId">
        /// Id of the Mpps object<paramref name="performedProcedureStep"/>
        /// </param>
        void ReplaceMpps(DicomObject performedProcedureStep, string ppsId);

        /// <summary>
        /// Update Pps
        /// </summary>
        /// <param name="modifiedPpsObject">dicomObject containing the delta changes of the pps</param>
        /// <param name="ppsId">
        /// Id of the Mpps object<paramref name="modifiedPpsObject"/>
        /// </param>
        void UpdateMpps(
            AIIDicomObject modifiedPpsObject,
            string ppsId
        );

        /// <summary>
        /// Delete performed procedure step
        /// </summary>
        /// <param name="ppsId">performed procedure step id</param>
        /// <exception cref="DeviceAccessException">
        /// Thrown when the underlying storage device encounters access problems
        /// </exception>
        void DeletePerformedProcedureStep(string ppsId);
    }
}