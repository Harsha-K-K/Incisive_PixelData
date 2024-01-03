using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.ApplicationIntegration.Log;
using Philips.Platform.Common;
using Philips.Platform.Common.DataAccess;
using Philips.Platform.StorageDevices.Utilities;
using Philips.Platform.StorageDevicesClient;
using Philips.Platform.Tracing;

using AIPDicomObject = Philips.Platform.Dicom.Data.DicomObject;

namespace PixelDataImplementation
{
    /// <summary>
    /// Helper class used by <see cref="CompositeStore" />
    /// This is a generic class which redirects the query to database
    /// or file store respectively
    /// </summary>
    internal class QueryHelper
    {

        private readonly IDatabaseStore databaseStore;

        private readonly FileStoreBase fileStore;

        private static readonly Tracer tracer = Tracer.CreateTracer(typeof(QueryHelper));

        private static readonly Tracer perfTracer =
            Tracer.CreateTracer(TraceCategory.Performance, typeof(QueryHelper));

        /// <summary>
        /// Initializes a new instance of QueryHelper class with given
        /// <see cref="FileStore"/> and <see cref="IDatabaseStore"/>
        /// </summary>
        /// <param name="databaseStore">the database store</param>
        /// <param name="fileStore">the file store</param>
        public QueryHelper(IDatabaseStore databaseStore, FileStoreBase fileStore)
        {
            this.databaseStore = databaseStore;
            this.fileStore = fileStore;
        }

        /// <summary>
        /// Gets the children count.
        /// </summary>
        /// <param name="queryLevel">The query level.</param>
        /// <param name="parentIdentifier">The parent identifier</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// Count of the children returned after making the query.
        /// </returns>
        public int GetChildrenCount(
            QueryLevel queryLevel,
            Identifier parentIdentifier,
            QueryFilter filter
        )
        {
            var childrenCount = 0;
            var storageKey =
                FileStoreUtilities.CreateStorageKeyFrom(databaseStore.DeviceId, parentIdentifier);
            switch (queryLevel)
            {
                case QueryLevel.Study:
                    childrenCount = GetChildrenCount(storageKey, Level.Study, queryLevel, filter);
                    break;
                case QueryLevel.Series:
                    childrenCount = GetChildrenCount(storageKey, Level.Series, queryLevel, filter);
                    break;
                case QueryLevel.Image:
                    var imagesInstanceUidsFromDmb = StorageRecordHelper.GetAllImagesInstanceUidUnderSeries(
                        parentIdentifier, storageKey, databaseStore.DeviceId);
                    childrenCount = imagesInstanceUidsFromDmb.Distinct().Count();
                    break;
                case QueryLevel.Frame:
                    //Get the image object and read the frame information.
                    DicomObject imageDicomFromDmb =
                        DataModelBuffer.GetImage(storageKey, DataLoadingMode.FullHeaders);
                    if (imageDicomFromDmb == null)
                    {
                        var fetchResults = fileStore.FetchImage(
                            storageKey.Identifier,
                            storageKey.Identifier.SeriesInstanceUid,
                            new List<string> { storageKey.Identifier.SopInstanceUid },
                            DataLoadingMode.FullHeaders
                        );
                        imageDicomFromDmb = fetchResults[0].DicomObjectHeader;
                    }

                    if (imageDicomFromDmb != null)
                    {
                        int? count =
                            imageDicomFromDmb.GetInt32(DicomDictionary.DicomNumberOfFrames);
                        if (count.HasValue)
                        {
                            childrenCount = count.Value;
                        }
                    }
                    break;
            }
            return childrenCount;
        }



        private int GetChildrenCount(
            StorageKey storageKey,
            Level dbQueryLevel,
            QueryLevel level,
            QueryFilter filter
        )
        {
            // Get instance uid's from datamodel buffer.
            var instanceUidsFromDmb =
                DataModelBuffer.GetChildrenInstanceUids(storageKey, level);

            // Get instance uid's from database.
            var instanceUidsFromDb = databaseStore.GetInstanceUids(
                dbQueryLevel, storageKey.Identifier, filter);

            instanceUidsFromDmb.AddRange(instanceUidsFromDb);
            return instanceUidsFromDmb.Distinct().Count();
        }

        // Get children instance uid's.
        internal List<string> GetChildrenInstanceUids(
            Level level,
            StorageKey storageKey,
            bool includeDmbResults = true
        )
        {
            List<string> instanceUidsFromDmb = null;
            if (includeDmbResults)
            {
                // Get instance uid's from datamodel buffer.
                instanceUidsFromDmb =
                    DataModelBuffer.GetChildrenInstanceUids(storageKey, level.GetQueryLevel());
            }

            // Get instance uid's from database.
            var instanceUidsFromDb = databaseStore.GetInstanceUids(
                level, storageKey.Identifier, QueryFilter.MatchAll());

            if (instanceUidsFromDmb != null)
            {
                instanceUidsFromDb.AddRange(instanceUidsFromDmb);
            }
            return instanceUidsFromDb.Distinct().ToList();
        }

        /// <summary>
        /// Gets the SOP class uid and count of each image type for the given series identifier.
        /// </summary>
        /// <param name="seriesIdentifier">The series identifier</param>
        /// <returns>
        /// Dictionary of SOP class uid and count of occurrences.
        /// </returns>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        internal IDictionary<string, int> GetSopClassInformation(Identifier seriesIdentifier)
        {
            var sopClassList = new Dictionary<string, int>();
            var storageKey =
                FileStoreUtilities.CreateStorageKeyFrom(databaseStore.DeviceId, seriesIdentifier);

            var imagesFromDmb = DataModelBuffer.GetAllImages(storageKey);
            foreach (var image in imagesFromDmb)
            {
                string sopClassUid = image.GetString(DicomDictionary.DicomSopClassUid);
                int count = sopClassList.ContainsKey(sopClassUid) ? sopClassList[sopClassUid] : 0;
                count++;
                sopClassList[sopClassUid] = count;
            }

            //Get records from storage records
            var sopDataFromStoredRecords = fileStore.GetSopClassInformation(seriesIdentifier);

            //Merge the data from DMB and stored records
            foreach (var sopClassUid in sopDataFromStoredRecords.Keys)
            {
                int count = sopClassList.ContainsKey(sopClassUid) ? sopClassList[sopClassUid] : 0;
                count += sopDataFromStoredRecords[sopClassUid];
                sopClassList[sopClassUid] = count;
            }
            return sopClassList;
        }

        /// <summary>
        /// Queries DICOM data from the storage device based on the filter
        /// </summary>
        /// <param name="level">
        /// The Query Level to be considered for returning the tags
        /// </param>
        /// <param name="parentIdentifier">
        /// parentIdentifier that restricts the query under its hierarchy
        /// </param>
        /// <param name="filter">
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
        /// A list of <see cref="PersistentDicomObject"/>. 
        /// <see cref="PersistentDicomObject"/> contains a <see cref="StorageKey"/> and
        /// <see cref="DicomObject"/>.
        /// </returns> 
        /// <remarks>At this point query on range on data is not supported</remarks>
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
            Stopwatch queryTimer = new Stopwatch();
            queryTimer.Start();

            //to measure individual performance measurement of perf.
            //critical operations within this function.
            //todo : currently we only measure time for file store ops.
            //todo : we do not measure the time for study/series level
            //todo : query which involve the sql database queries.
            //todo : if we need to do that also, then a minimal interface change
            //todo : is required IDatabaseStore.QueryData should also 
            //todo : take a perfTimer as input, and the perfTimer should
            //todo : also be enhanced to contain sql level performance timers.
            PerformanceTimer perfTimer = new PerformanceTimer();

            string studyId = string.Empty;
            if (parentIdentifier != null)
            {
                studyId = parentIdentifier.StudyInstanceUid;
            }
            // study id is NULL when query level is patient ...
            tracer.TraceVerbose("Entering QueryHelper.QueryData: Level = " + level
                + "StudyInstanceUid = " + studyId.Anonymize());
            if (tracer.IsVerboseOn)
            {
                tracer.TraceVerbose(string.Format(CultureInfo.InvariantCulture,
                    "query for {0} for level--{1} ", parentIdentifier?.Anonymize() ?? string.Empty,
                    level));
            }

            bool onlyCompletedResults =
                ConfigurationManager.Instance.GlobalConfiguration.IgnoreInProgressRecords;

            QueryResult queryResults = null;
            switch (level)
            {
                //todo : query at patient level only considers the committed patients
                //todo : it does not include the patients that are present in data model buffer.
                //todo : This is currently so because we do not have the level of patients
                //todo : at the data model buffer level. 
                //todo : We need to either introduce a level called patients in dmb (or)
                //todo : we need to retrieve studies from dmb and construct patients from that
                case QueryLevel.Patient:
                    var resultStream = databaseStore.QueryData(
                        Level.Patient,
                        parentIdentifier,
                        filter,
                        sortCriteria,
                        sortOrder,
                        maxRecords
                    );
                    queryResults = new QueryResult(resultStream);
                    break;

                case QueryLevel.Study:
                    IList<AIPDicomObject> queryResultsFromDmb = new List<AIPDicomObject>();
                    if (!onlyCompletedResults)
                    {
                        queryResultsFromDmb = DataModelBuffer.GetStudies(
                            databaseStore.DeviceId,
                            FileStoreUtilities.GetPatientKey(parentIdentifier),
                            filter
                        );
                    }

                    var studyQueryResultsFromDb = new QueryResult(
                        databaseStore.QueryData(
                            Level.Study,
                            parentIdentifier,
                            filter,
                            sortCriteria,
                            sortOrder,
                            maxRecords,
                            onlyCompletedResults
                        )
                    );

                    var studyDicomObjetsFromDb = StorageDevicesUtilities.ExtractDicomObjects(
                        studyQueryResultsFromDb, QueryLevel.Study);

                    var resultsFromDbAndDmb = FileStoreUtilities.UnionOf(
                        queryResultsFromDmb,
                        studyDicomObjetsFromDb,
                        DicomDictionary.DicomStudyInstanceUid
                        );

                    queryResults = new QueryResult(resultsFromDbAndDmb);
                    break;

                case QueryLevel.Series:
                    //todo : for series level query, we have to first get the results from dmb;
                    //todo : and then get the results from db; and then remove duplicates from the 
                    //todo : results (this should be done the same way it is done for study 
                    //todo : level query).
                    IList<AIPDicomObject> seriesDosFromDmb = new List<AIPDicomObject>();
                    if (!onlyCompletedResults)
                    {
                        seriesDosFromDmb = GetSeriesDicomObjectsFromDataModelBuffer(
                            databaseStore.DeviceId, parentIdentifier, filter);
                    }

                    IList<AIPDicomObject> seriesDicomObjects;

                    //if the query filter contains any SQ tag, then we will first get the storables
                    //and then use the fast_access dicom objects just retrieved to filter based on 
                    //the queryfilter. This of course assumes that the fast access atributes
                    //will contain all the queryable attributes. 
                    //todo : check if the above is indeed the case
                    //todo : the same approach can be replicated for study level 
                    if (FilterMatcher.ContainsSequenceFilter(filter))
                    {
                        var unfilteredResultStream = databaseStore.QueryData(
                            Level.Series,
                            parentIdentifier,
                            QueryFilter.MatchAll(),
                            sortCriteria,
                            sortOrder,
                            //todo : max records may be more than the actual records returned.
                            //todo : so we need a way to specify a record count to the 
                            //todo : query so that even after filtering, we get 'maxRecords' count
                            //todo : returned to the caller.
                            maxRecords,
                            onlyCompletedResults
                            );
                        var unfilteredQueryResult = new QueryResult(unfilteredResultStream);
                        var unFilteredSeriesDicomObjects =
                            StorageDevicesUtilities.ExtractDicomObjects(
                                unfilteredQueryResult, level);
                        seriesDicomObjects = unFilteredSeriesDicomObjects.Where(s => {
                            return FilterMatcher.SatisfiesFilter(s, filter);
                        }).ToList();
                    }
                    else
                    {
                        var seriesQueryResultsFromDb = new QueryResult(
                            databaseStore.QueryData(
                                Level.Series,
                                parentIdentifier,
                                filter,
                                sortCriteria,
                                sortOrder,
                                maxRecords,
                                onlyCompletedResults
                            )
                        );

                        //get dicom objects from db.
                        seriesDicomObjects = StorageDevicesUtilities.ExtractDicomObjects(
                            seriesQueryResultsFromDb, QueryLevel.Series
                            );
                    }

                    seriesDicomObjects = FileStoreUtilities.UnionOf(
                        seriesDosFromDmb,
                        seriesDicomObjects,
                        DicomDictionary.DicomSeriesInstanceUid
                        );

                    //todo : we should group all the attributes which need path resolution 
                    //todo : in a list. So that the updates can be applied to all of them
                    //todo : from the same place and also it will be easier to extend this in 
                    //todo : the future.
                    foreach (var obj in seriesDicomObjects)
                    {
                        StorageDevicesUtilities.ResolveRepresentativeImagePath(
                            obj, databaseStore.DeviceId);
                    }
                    queryResults = new QueryResult(seriesDicomObjects);
                    break;

                case QueryLevel.Image:
                    // ParentIdentifier can be Null for specific use cases like reports in portal
                    if (parentIdentifier == null)
                    {
                        if (filter != QueryFilter.MatchAll())
                        {
                            using (
                                Stream results =
                                    databaseStore.QueryData(
                                        Level.Series,
                                        null,
                                        filter,
                                        sortCriteria,
                                        sortOrder,
                                        maxRecords
                                    )
                            )
                            {
                                if (results != null && results.Length > 0)
                                {
                                    IList<AIPDicomObject> seriesObjects =
                                        StorageDevicesUtilities.ExtractDicomObjects(results);
                                    queryResults = QueryAllImages(filter, seriesObjects);
                                }
                            }
                        }
                        else
                        {
                            throw new NotSupportedException(
                                "Image query is not supported with match all filter.");
                        }
                    }
                    else
                    {
                        //todo : for measuring the performance, we can consider passing
                        //todo : in the perfTimer object to GetImages
                        //todo : and that will accumulate the amount of time spent in 
                        //todo : deserializing dicom headers. We can do this, if we 
                        //todo : run into a performance problem and we are not sure
                        //todo : where the time is spent in query_images even after
                        //todo : looking at existing performance traces which measure
                        //todo : the time taken to query from file store.
                        var imagesFromDmb = DataModelBuffer.GetImages(
                            new StorageKey(databaseStore.DeviceId, parentIdentifier), filter
                            );
                        if (imagesFromDmb != null && imagesFromDmb.Count > 0)
                        {
                            foreach (var queryDicomObject in imagesFromDmb)
                            {
                                StorageDevicesUtilities.ResolveBulkDataReferenceFilePath(
                                    queryDicomObject,
                                    databaseStore.DeviceId,
                                    perfTimer.ResolveBDRTimer
                                );
                            }
                        }

                        QueryResult imageQueryResultsFromDb = GetImageLevelFastAccess(
                            parentIdentifier, filter, maxRecords, recordOffset, perfTimer);

                        if (tracer.IsVerboseOn)
                        {
                            tracer.TraceVerbose(string.Format(CultureInfo.InvariantCulture,
                                "query for {0} for level--{1} ",
                                parentIdentifier.Anonymize(),
                                level));
                        }

                        resultsFromDbAndDmb = FileStoreUtilities.UnionOf(
                            imagesFromDmb,
                            imageQueryResultsFromDb.QueryResultList,
                            DicomDictionary.DicomSopInstanceUid
                        );
                        queryResults = new QueryResult(resultsFromDbAndDmb);
                    }
                    break;

                case QueryLevel.Frame:
                    if (parentIdentifier != null)
                    {
                        // Handle when ParentIdentifier not sopInstance.
                        queryResults = GetFrameLevelFastAccess(
                            parentIdentifier,
                            filter,
                            perfTimer
                        );
                    }

                    break;
            }

            tracer.TraceVerbose("Exiting QueryHelper.QueryData: Level = " + level
                + "StudyInstanceUid = " + studyId.Anonymize());

            queryTimer.Stop();

            //performance logging of individual performance critical operations
            //within fetch.
            if (level == QueryLevel.Image && perfTracer.IsVerboseOn)
            {
                string parentId = "";
                if (parentIdentifier != null)
                {
                    parentId = parentIdentifier.Anonymize();
                }
                string log = FileStoreUtilities.GetLogMsg(
                    "QueryHelper.Query => QueryLevel = " + level +
                    " Parent Identifier = " + parentId +
                    " Total query time = " + queryTimer.ElapsedMilliseconds +
                    " : Sub-operation split ::=> ", perfTimer
                );
                perfTracer.TraceVerbose(log);
            }

            return queryResults;
        }

        /// <summary>
        /// Query stream
        ///</summary> 
        public MemoryStream QueryDataAsStream(
            QueryLevel level,
            Identifier parentIdentifier,
            QueryFilter filter,
            DictionaryTagsCollection sortCriteria = null,
            QuerySortOrder sortOrder = QuerySortOrder.None,
            int maxRecords = 200
        )
        {
            MemoryStream queryResultStream = new MemoryStream();
            bool onlyCompletedResults =
                ConfigurationManager.Instance.GlobalConfiguration.IgnoreInProgressRecords;
            switch (level)
            {
                case QueryLevel.Patient:
                    // study id is NULL when query level is patient.
                    queryResultStream = (MemoryStream)databaseStore.QueryData(
                        Level.Patient,
                        parentIdentifier,
                        filter,
                        sortCriteria,
                        sortOrder,
                        maxRecords
                    );
                    break;

                case QueryLevel.Study:
                    var studyDmbObjects = DataModelBuffer.GetStudies(
                        databaseStore.DeviceId,
                        FileStoreUtilities.GetPatientKey(parentIdentifier),
                        filter
                    );
                    var studyStream = (MemoryStream)databaseStore.QueryData(
                        Level.Study,
                        parentIdentifier,
                        filter,
                        sortCriteria,
                        sortOrder,
                        maxRecords,
                        onlyCompletedResults
                    );

                    // Append the DB stream with DMB objects as stream
                    if (studyDmbObjects != null && studyDmbObjects.Count > 0)
                    {
                        studyStream = AppendInstancesStreamWithDicomObjects(
                            studyDmbObjects, studyStream);
                    }

                    queryResultStream = studyStream;
                    break;

                case QueryLevel.Series:
                    var seriesDmbObjects = GetSeriesDicomObjectsFromDataModelBuffer(
                        databaseStore.DeviceId,
                        parentIdentifier,
                        filter
                    );

                    MemoryStream seriesStream;
                    if (FilterMatcher.ContainsSequenceFilter(filter))
                    {
                        seriesStream = (MemoryStream)databaseStore.QueryData(
                            Level.Series,
                            parentIdentifier,
                            QueryFilter.MatchAll(),
                            sortCriteria,
                            sortOrder,
                            maxRecords,
                            onlyCompletedResults
                        );
                    }
                    else
                    {
                        seriesStream = (MemoryStream)databaseStore.QueryData(
                            Level.Series,
                            parentIdentifier,
                            filter
                        );
                    }

                    // Append the DB stream with DMB objects as stream
                    if (seriesDmbObjects != null && seriesDmbObjects.Count > 0)
                    {
                        seriesStream = AppendInstancesStreamWithDicomObjects(
                            seriesDmbObjects, seriesStream);
                    }

                    queryResultStream = seriesStream;
                    break;
            }
            return queryResultStream;
        }

        internal IList<MemoryStream> QueryImagesAsStream(
            Identifier parentIdentifier,
            QueryFilter queryFilter
        )
        {
            IList<MemoryStream> queryResultStreams = new List<MemoryStream>();
            // Query at study level
            if (string.IsNullOrWhiteSpace(parentIdentifier.SeriesInstanceUid))
            {
                var queryResult =
                    QueryData(QueryLevel.Series, parentIdentifier, QueryFilter.MatchAll());
                IList<AIPDicomObject> seriesObjects =
                    StorageDevicesUtilities.ExtractDicomObjects(queryResult);
                //For each series level fast access object, query for image fast access object.
                //Storage key has to be populated for each image fast access object.
                foreach (AIPDicomObject seriesObject in seriesObjects)
                {
                    Identifier seriesIdentifier = Identifier.CreateSeriesIdentifier(
                        Identifier.CreatePatientKeyFromDicomObject(seriesObject),
                        seriesObject.GetString(DicomDictionary.DicomStudyInstanceUid),
                        seriesObject.GetString(DicomDictionary.DicomSeriesInstanceUid));
                    queryResultStreams.Add(
                        QueryImagesUnderSeriesAsStream(seriesIdentifier, queryFilter));
                }
            }
            else
            {
                queryResultStreams.Add(
                    QueryImagesUnderSeriesAsStream(parentIdentifier, queryFilter));
            }
            return queryResultStreams;
        }

        private MemoryStream QueryImagesUnderSeriesAsStream(
            Identifier parentIdentifier,
            QueryFilter queryFilter
        )
        {
            if (string.IsNullOrWhiteSpace(parentIdentifier.SeriesInstanceUid))
            {
                const string errorMessage =
                    "Series instance uid in parent identifier is null or blank.";
                tracer.TraceInfo(errorMessage);
                throw new ArgumentException(errorMessage, "parentIdentifier");
            }

            StorageKey parentStorageKey = new StorageKey(databaseStore.DeviceId, parentIdentifier);
            // Get query results from DMB
            var imageDmbObjects = DataModelBuffer.GetImages(parentStorageKey, queryFilter);
            if (imageDmbObjects != null)
            {
                foreach (var imageDmbObject in imageDmbObjects)
                {
                    //Remove pixel data
                    imageDmbObject.Remove(DicomDictionary.DicomPixelData);
                }
            }

            // Get fast header file contents as memory stream
            var imagesStream = fileStore.QueryImageStream(parentIdentifier);

            // Append the FS stream with DMB objects as stream
            if (imageDmbObjects != null && imageDmbObjects.Count > 0)
            {
                imagesStream = AppendInstancesStreamWithDicomObjects(imageDmbObjects, imagesStream);
            }

            if (imagesStream != null)
            {
                imagesStream.Position = 0;
            }
            return imagesStream;
        }

        internal IList<QueryInformation> QueryFramesAsStream(Identifier parentIdentifier)
        {
            IList<QueryInformation> queryInformation = new List<QueryInformation>();
            // Query at study level
            if (string.IsNullOrWhiteSpace(parentIdentifier.SeriesInstanceUid))
            {
                var queryResult =
                    QueryData(QueryLevel.Series, parentIdentifier, QueryFilter.MatchAll());
                IList<AIPDicomObject> seriesObjects =
                    StorageDevicesUtilities.ExtractDicomObjects(queryResult);
                //For each series level fast access object, query for image fast access object.
                //Storage key has to be populated for each image fast access object.
                foreach (AIPDicomObject seriesObject in seriesObjects)
                {
                    Identifier seriesIdentifier = Identifier.CreateSeriesIdentifier(
                        Identifier.CreatePatientKeyFromDicomObject(seriesObject),
                        seriesObject.GetString(DicomDictionary.DicomStudyInstanceUid),
                        seriesObject.GetString(DicomDictionary.DicomSeriesInstanceUid));
                    queryInformation.Add(
                        QueryFramesUnderSeriesAsStream(seriesIdentifier));
                }
            }
            else
            {
                queryInformation.Add(
                    QueryFramesUnderSeriesAsStream(parentIdentifier));
            }
            return queryInformation;
        }

        private QueryInformation QueryFramesUnderSeriesAsStream(Identifier parentIdentifier)
        {
            if (string.IsNullOrWhiteSpace(parentIdentifier.SeriesInstanceUid))
            {
                const string errorMessage =
                    "Series instance uid in parent identifier is null or blank.";
                tracer.TraceInfo(errorMessage);
                throw new ArgumentException(errorMessage, "parentIdentifier");
            }

            return fileStore.QueryFrameStream(parentIdentifier);
        }

        private static MemoryStream AppendInstancesStreamWithDicomObjects(
            List<AIPDicomObject> dicomObjects,
            MemoryStream instancesStream
        )
        {
            if (instancesStream == null)
            {
                instancesStream = new MemoryStream();
            }

            instancesStream.Seek(0, SeekOrigin.End);
            foreach (var dicomObject in dicomObjects)
            {
                byte[] byteArray = FileStoreHelper.Serialize(dicomObject);
                instancesStream.Write(byteArray, 0, byteArray.Length);
            }
            instancesStream.Seek(0, SeekOrigin.Begin);
            return instancesStream;
        }

        private static List<AIPDicomObject> GetSeriesDicomObjectsFromDataModelBuffer(
            string deviceId,
            Identifier parentIdentifier,
            QueryFilter filter
        )
        {
            List<StorageKey> parentStudyStorageKeys = new List<StorageKey>();

            if (parentIdentifier == null)
            { //get series from under all studies
                var studyKeys = DataModelBuffer.GetAllStudyStorageKeys(deviceId);
                parentStudyStorageKeys.AddRange(studyKeys);
            }
            else
            { //get series from only under the given study(ies)
                parentStudyStorageKeys.AddRange(
                    FileStoreUtilities.CreateStudyStorageKeysFrom(deviceId, parentIdentifier)
                );
            }

            List<AIPDicomObject> matchingSeriesDicomObjects = new List<AIPDicomObject>();
            foreach (var parentKey in parentStudyStorageKeys)
            {
                var seriesObjects = DataModelBuffer.GetSeries(parentKey, filter);
                matchingSeriesDicomObjects.AddRange(seriesObjects);
            }

            return matchingSeriesDicomObjects;
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
        public IList<DicomObject> QuerImagesForSeries(
            Identifier parentIdentifier,
            QueryFilter filters
        )
        {
            IList<DicomObject> imageDicomList =
                GetImageLevelFastAccessAsDicomObjects(parentIdentifier, filters);

            // todo - do not copy
            var images = DataModelBuffer.GetImages(
                new StorageKey(databaseStore.DeviceId, parentIdentifier), filters
            );

            foreach (var image in images)
            {
                imageDicomList.Add(image);
            }

            return imageDicomList;
        }

        /// <summary>
        /// Gets the image level fast access.
        /// </summary>
        /// <param name="parentIdentifier">The parent identifier.</param>
        /// <param name="filters">The filters.</param>
        /// <returns>
        /// The image query results
        /// </returns>
        private IList<DicomObject> GetImageLevelFastAccessAsDicomObjects(
            Identifier parentIdentifier,
            QueryFilter filters
        )
        {
            tracer.TraceVerbose("Entering QueryHelper.GetImageLevelFastAccess");

            if (parentIdentifier == null)
            {
                const string errorMessage = "Parent identifier cannot be null";
                tracer.TraceInfo(errorMessage);
                throw new ArgumentNullException("parentIdentifier", errorMessage);
            }

            if (string.IsNullOrWhiteSpace(parentIdentifier.SeriesInstanceUid))
            {
                throw new ArgumentNullException(
                    "parentIdentifier", "Series instance uid cannot be null");
            }

            IList<DicomObject> imageList =
                fileStore.QueryImageAsDicom(parentIdentifier, filters);

            tracer.TraceVerbose("Exiting QueryHelper.GetImageLevelFastAccess");
            return imageList;
        }


        /// <summary>
        /// Gets the image level fast access.
        /// </summary>
        /// <param name="parentIdentifier">The parent identifier.</param>
        /// <param name="filters">The filters.</param>
        /// <param name="recordOffset"></param>
        /// <param name="perfTimer">
        /// The performance timer which timer performance critical operations
        /// within this function.
        /// </param>
        /// <param name="maxRecords"></param>
        /// <returns>
        /// The image query results
        /// </returns>
        private QueryResult GetImageLevelFastAccess(
            Identifier parentIdentifier,
            QueryFilter filters,
            int maxRecords,
            int recordOffset,
            PerformanceTimer perfTimer
        )
        {
            tracer.TraceVerbose("Entering QueryHelper.GetImageLevelFastAccess");
            QueryResult queryResult = new QueryResult();

            // ParentIdentifer cannot be null. For a query from AII, a non-null identifier needs to 
            // be passed for Image level
            if (string.IsNullOrWhiteSpace(parentIdentifier.SeriesInstanceUid))
            {
                //Series instance uid is null, hence query for series level using the
                //parent identifier's study instance uid
                Stream fastAccessStream =
                    databaseStore.QueryData(
                    Level.Series,
                    parentIdentifier,
                    QueryFilter.MatchAll());

                IList<AIPDicomObject> seriesObjects =
                    StorageDevicesUtilities.ExtractDicomObjects(fastAccessStream);

                //For each series level fast access object, query for image fast access object.
                //Storage key has to be populated for each image fast access object.
                foreach (AIPDicomObject seriesObject in seriesObjects)
                {
                    Identifier seriesIdentifier =
                        Identifier.CreateSeriesIdentifier(
                            Identifier.CreatePatientKeyFromDicomObject(seriesObject),
                            seriesObject.GetString(DicomDictionary.DicomStudyInstanceUid),
                            seriesObject.GetString(DicomDictionary.DicomSeriesInstanceUid));
                    queryResult.Combine(
                        fileStore.QueryImage(
                            seriesIdentifier, filters, Int32.MaxValue, 0, perfTimer)
                    );
                }
            }
            else
            {
                queryResult.Combine(
                    fileStore.QueryImage(
                        parentIdentifier, filters, maxRecords, recordOffset, perfTimer));
            }
            tracer.TraceVerbose("Exiting QueryHelper.GetImageLevelFastAccess");
            return queryResult;
        }

        /// <summary>
        /// Gets the image level fast access for all images
        /// </summary>
        /// <param name="filters">The filters.</param>
        /// <param name="seriesObjects"></param>
        /// <returns>
        /// The image query results
        /// </returns>
        private QueryResult QueryAllImages(
            QueryFilter filters,
            IList<AIPDicomObject> seriesObjects
        )
        {
            tracer.TraceVerbose("Entering QueryHelper.QueryAllImages");
            QueryResult queryResult = new QueryResult();
            //For each series level fast access object, query for image fast access objects.
            foreach (AIPDicomObject seriesObject in seriesObjects)
            {
                //Storage key has to be populated for each series fast access object.
                Identifier seriesIdentifier = Identifier.CreateSeriesIdentifier(
                    Identifier.CreatePatientKeyFromDicomObject(seriesObject),
                    seriesObject.GetString(DicomDictionary.DicomStudyInstanceUid),
                    seriesObject.GetString(DicomDictionary.DicomSeriesInstanceUid));
                queryResult.Combine(
                    fileStore.QueryImage(
                        seriesIdentifier, filters, Int32.MaxValue, 0, null, false));
            }
            tracer.TraceVerbose("Exiting QueryHelper.QueryAllImages");
            return queryResult;
        }


        bool IsImagePresent(Identifier parentIdentifier, QueryFilter filters)
        {
            tracer.TraceVerbose("Entering QueryHelper.IsImagePresent");
            bool imagePresent = false;

            try
            {
                // ParentIdentifer cannot be null. For a query from AII, a non-null identifier 
                // needs to be passed for Image level
                if (string.IsNullOrWhiteSpace(parentIdentifier.SeriesInstanceUid))
                {
                    //Series instance uid is null, hence query for series level using the
                    //parent identifier's study instance uid
                    Stream fastAccessStream = databaseStore.QueryData(
                        Level.Series,
                        parentIdentifier,
                        QueryFilter.MatchAll());

                    IList<AIPDicomObject> seriesObjects =
                        StorageDevicesUtilities.ExtractDicomObjects(fastAccessStream);

                    //For each series level fast access object, query for image fast access object.
                    //Storage key has to be populated for each image fast access object.
                    foreach (AIPDicomObject seriesObject in seriesObjects)
                    {
                        Identifier seriesIdentifier =
                            Identifier.CreateSeriesIdentifier(
                                Identifier.CreatePatientKeyFromDicomObject(seriesObject),
                                seriesObject.GetString(DicomDictionary.DicomStudyInstanceUid),
                                seriesObject.GetString(DicomDictionary.DicomSeriesInstanceUid));
                        imagePresent = fileStore.IsImagePresent(seriesIdentifier, filters);
                        if (imagePresent)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    imagePresent = fileStore.IsImagePresent(parentIdentifier, filters);
                }
            }
            catch (DataCorruptException e)
            {
                // ignore this exception in this case since it checks for image present
                // there are chances that image header files may not be available.
                StorageDevicesUtilities.LogError("Failed to check image presence.", e);
                UtilizationLogger.LogContext(LoggingConstants.DatabaseModuleId,
                    LoggingConstants.DataCorruptionExceptionEventId,
                    new UtilizationLogData
                    {
                        DateTime = DateTime.Now,
                        MachineName = Environment.MachineName,
                        DescriptionParameters = new[] {
                            "DataCorruptionException"
                        }
                    });
            }
            tracer.TraceVerbose("Exiting QueryHelper.GetImageLevelFastAccess");
            return imagePresent;
        }

        /// <summary>
        /// Gets the frame level data, in case the parent identifier does not specify sop
        /// </summary>
        /// <param name="parentIdentifier">The parent identifier.</param>
        /// <param name="filters">The filters.</param>
        /// <param name="perfTimer"></param>
        /// <returns>
        /// The frame query results
        /// </returns>
        private QueryResult GetFrameLevelFastAccess(
            Identifier parentIdentifier,
            QueryFilter filters,
            PerformanceTimer perfTimer = null
        )
        {
            tracer.TraceVerbose("Entering QueryHelper.GetFrameLevelFastAccess");
            QueryResult queryResult = new QueryResult();

            var seriesLock = LockFactory.GetSeriesLock(
                databaseStore.DeviceId,
                parentIdentifier.StudyInstanceUid,
                parentIdentifier.SeriesInstanceUid);

            try
            {
                seriesLock.EnterReadLock();

                // ParentIdentifer cannot be null. For a query from AII, a non-null 
                // identifier needs to be passed for Frame level.
                if (string.IsNullOrWhiteSpace(parentIdentifier.SeriesInstanceUid))
                {
                    //Series instance uid is null, hence query for series level using the
                    //parent identifier's study instance uid
                    Stream fastAccessStream =
                        databaseStore.QueryData(Level.Series, parentIdentifier, filters);

                    IList<AIPDicomObject> seriesObjects =
                        StorageDevicesUtilities.ExtractDicomObjects(fastAccessStream);

                    //For each series level fast access object, query for image fast access object.
                    //Storage key has to be populated for each image fast access object.
                    foreach (var seriesObject in seriesObjects)
                    {
                        Identifier seriesIdentifier =
                            Identifier.CreateSeriesIdentifier(
                                Identifier.CreatePatientKeyFromDicomObject(seriesObject),
                                seriesObject.GetString(DicomDictionary.DicomStudyInstanceUid),
                                seriesObject.GetString(DicomDictionary.DicomSeriesInstanceUid));
                        queryResult.Combine(
                            GetFrameLevelFastAccessFromSopUid(
                                seriesIdentifier, filters, perfTimer
                            )
                        );
                    }
                }
                else if (string.IsNullOrWhiteSpace(parentIdentifier.SopInstanceUid))
                {
                    queryResult.Combine(
                        GetFrameLevelFastAccessFromSopUid(
                            parentIdentifier, filters, perfTimer
                        )
                    );
                }
                else
                {
                    queryResult.Combine(
                        fileStore.QueryFrame(
                            parentIdentifier, filters, null, perfTimer
                        )
                    );
                }

                tracer.TraceVerbose("Exiting QueryHelper.GetFrameLevelFastAccess");
                return queryResult;
            }
            finally
            {
                if (seriesLock != null)
                {
                    using (seriesLock)
                    {
                        seriesLock.ExitReadLock();
                    }
                }
            }
        }

        /// <summary>
        /// Fetch frame level fast access objects, given a sop instance uid.
        /// </summary>
        /// <param name="seriesIdentifier">The series identifier.</param>
        /// <param name="filters">The filters.</param>
        /// <param name="perfTimer">
        /// Times the performance critical operations.
        /// </param>
        /// <returns>
        /// The frame query results
        /// </returns>
        private QueryResult GetFrameLevelFastAccessFromSopUid(
            Identifier seriesIdentifier,
            QueryFilter filters,
            PerformanceTimer perfTimer
        )
        {
            tracer.TraceVerbose("Entering QueryHelper.GetFrameLevelFastAccessFromSopUid");
            QueryResult queryResult = new QueryResult();
            QueryResult imageQueryResults =
                fileStore.QueryImage(seriesIdentifier, filters, Int32.MaxValue, 0, perfTimer);
            IList<AIPDicomObject> imageObjects =
                StorageDevicesUtilities.ExtractDicomObjects(imageQueryResults);
            foreach (var imageObject in imageObjects)
            {
                Identifier imageIdentifier =
                    Identifier.CreateImageIdentifier(
                        Identifier.CreatePatientKeyFromDicomObject(imageObject),
                        imageObject.GetString(DicomDictionary.DicomStudyInstanceUid),
                        imageObject.GetString(DicomDictionary.DicomSeriesInstanceUid),
                        imageObject.GetString(DicomDictionary.DicomSopInstanceUid)
                    );
                queryResult.Combine(
                    fileStore.QueryFrame(imageIdentifier, filters, imageObject, perfTimer)
                );
            }
            tracer.TraceVerbose("Exiting QueryHelper.GetFrameLevelFastAccessFromSopUid");
            return queryResult;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="parentIdentifier"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public bool IsObjectPresent(
            QueryLevel level,
            Identifier parentIdentifier,
            QueryFilter filter)
        {
            if (tracer.IsVerboseOn)
            {
                tracer.TraceVerbose("Entering QueryHelper.IsObjectPresent");
                tracer.TraceVerbose(string.Format(CultureInfo.InvariantCulture,
                    "query for {0} for level--{1} ", parentIdentifier.Anonymize(), level));
            }
            bool hasObject = false;
            QueryResult queryResults;
            switch (level)
            {
                case QueryLevel.Study:
                    List<AIPDicomObject> studies =
                        DataModelBuffer.GetStudies(
                            databaseStore.DeviceId,
                            FileStoreUtilities.GetPatientKey(parentIdentifier),
                            filter
                        );
                    if (studies.Count > 0)
                    {
                        return true;
                    }
                    //TODO - implement in DB to check and return value instead of returning results
                    queryResults =
                        new QueryResult(
                            databaseStore.QueryData(Level.Study, parentIdentifier, filter));
                    hasObject = queryResults.HasResults();
                    break;
                case QueryLevel.Series:
                    List<AIPDicomObject> series =
                        DataModelBuffer.GetSeries(
                            new StorageKey(databaseStore.DeviceId, parentIdentifier), filter);
                    if (series.Count > 0)
                    {
                        return true;
                    }
                    queryResults =
                        new QueryResult(
                            databaseStore.QueryData(Level.Series, parentIdentifier, filter));
                    hasObject = queryResults.HasResults();
                    break;
                case QueryLevel.Image:
                    var images = DataModelBuffer.GetImages(
                        new StorageKey(databaseStore.DeviceId, parentIdentifier), filter);
                    hasObject = (images != null && images.Count > 0) ||
                        IsImagePresent(parentIdentifier, filter);
                    break;
                case QueryLevel.Frame:
                    // Handle when ParentIdentifier not sopInstance.
                    queryResults =
                        GetFrameLevelFastAccess(parentIdentifier, filter);
                    hasObject = queryResults.HasResults();
                    break;

            }
            if (tracer.IsVerboseOn)
            {
                tracer.TraceVerbose("Exiting QueryHelper.IsObjectPresent");
            }
            return hasObject;
        }

        /// <summary>
        /// Fetch study/series level dicom objects, given level and filter 
        /// </summary>
        /// <param name="level">Level</param>
        /// <param name="parentIdentifier">Patient id</param>
        /// <param name="filter">Query Filter</param>
        /// <returns></returns>
        private PersistentDicomObjectCollection Query(
            Level level,
            Identifier parentIdentifier,
            QueryFilter filter
        )
        {
            string studyId = string.Empty;
            if (parentIdentifier != null)
            {
                studyId = parentIdentifier.StudyInstanceUid;
            }

            tracer.TraceVerbose("Entering QueryHelper.Query: Level = " + level
                + "StudyInstanceUid = " + studyId.Anonymize());

            if (level != Level.Study && parentIdentifier == null)
            {
                const string errorMessage = "Parent Identifier must not be null";
                StorageDevicesUtilities.LogError(errorMessage);
                throw new ArgumentNullException("parentIdentifier", errorMessage);
            }
            PersistentDicomObjectCollection result;
            if (
                level == Level.Study &&
                StorageDeviceProvider.GetDeviceType(databaseStore.DeviceId) ==
                SystemIntegration.DeviceTypes.DatabaseRepository
            )
            {
                QueryResult fastAccessObjects =
                    QueryData(
                    level.GetQueryLevel(),
                    parentIdentifier, filter);
                result =
                    StorageDevicesUtilities.ExtractResultsAsPDO(
                    databaseStore.DeviceId,
                    level.GetQueryLevel(),
                    fastAccessObjects,
                    true,
                    parentIdentifier);

            }
            else
            {

                QueryResult fastAccessObjects =
                    QueryData(
                    level.GetQueryLevel(),
                    parentIdentifier, filter);
                result =
                    StorageDevicesUtilities.ExtractResultsAsPDO(
                    databaseStore.DeviceId,
                    level.GetQueryLevel(),
                    fastAccessObjects,
                    true,
                    parentIdentifier);
            }
            tracer.TraceVerbose("Entering QueryHelper.Query: Level = " + level
                + "StudyInstanceUid = " + studyId.Anonymize());
            return result;
        }

        /// <summary>
        /// Gets all the studies for patientId
        /// </summary>
        /// <param name="patientId">the patient identifier</param>
        /// <param name="patientName">the patient name</param>
        /// <param name="issuerOfPatientId"></param>
        /// <param name="wildChar"></param>
        /// <returns></returns>
        public PersistentDicomObjectCollection GetStudiesForPatient(
            string patientId,
            string patientName,
            string issuerOfPatientId,
            string wildChar
        )
        {
            tracer.TraceVerbose("Entering QueryHelper.GetStudiesForPatient");
            List<QueryFilter> queryFilterArray = new List<QueryFilter> {
                QueryFilter.MatchAny(DicomDictionary.DicomPatientId, patientId)
            };

            //Fix for ISP00178746
            //Before appending wildcard, remove the trailing caret'^' from patient name 
            //because after appending wildcard, it will not be removed during SQL query
            if (!string.IsNullOrWhiteSpace(patientName))
            {
                patientName = PatientNameUtility.RemoveTrailingSuffixesFromAllGroups(patientName);
            }
            patientName += wildChar;
            queryFilterArray.Add(
                QueryFilter.MatchWildcard(DicomDictionary.DicomPatientName, patientName));
            if (!string.IsNullOrEmpty(issuerOfPatientId))
            {
                queryFilterArray.Add(QueryFilter.MatchAny(DicomDictionary.DicomIssuerOfPatientId,
                issuerOfPatientId));
            }
            QueryFilter qf = QueryFilter.And(queryFilterArray.ToArray());
            PersistentDicomObjectCollection pdo = Query(Level.Study, null, qf);
            tracer.TraceVerbose("Exiting QueryHelper.GetStudiesForPatient");
            return pdo;
        }

        /// <summary>
        /// Gets all the series for study
        /// </summary>
        /// <param name="studyId">the patient name</param>
        /// <returns></returns>
        public PersistentDicomObjectCollection GetAllSeries(Identifier studyId)
        {
            tracer.TraceVerbose("Entering QueryHelper.GetAllSeries");
            QueryFilter queryFilter = QueryFilter.MatchAny(
                DicomDictionary.DicomStudyInstanceUid, studyId.StudyInstanceUid);
            PersistentDicomObjectCollection po = Query(Level.Series, studyId, queryFilter);
            tracer.TraceVerbose("Exiting QueryHelper.GetAllSeries");
            return po;
        }

        /// <summary>
        /// Get instance storage key collections for a given filter under given study/series.
        /// </summary>
        internal IEnumerable<StorageKey> FindInstances(
            Identifier parentIdentifier, QueryFilter filter)
        {
            return fileStore.FindInstances(parentIdentifier, filter.Value.ToArray());
        }
    }
}