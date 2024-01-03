using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Philips.Platform.ApplicationIntegration.Authorization;
using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.ApplicationIntegration.Log;
using Philips.Platform.ApplicationIntegration.Tracing;
using Philips.Platform.Common;
using Philips.Platform.Common.DataAccess;
using Philips.Platform.CommonUtilities.Pooling;
using Philips.Platform.Connectivity;
using Philips.Platform.Dicom;
using Philips.Platform.Fhir.Custom;
using Philips.Platform.StateAggregationService;
using Philips.Platform.StorageDevices;
using Philips.Platform.StorageDevices.Reconciliation;
using Philips.Platform.StorageDevices.Utilities;
using Philips.Platform.SystemIntegration;
using Philips.Platform.SystemIntegration.Exceptions;
using Philips.Platform.Utilities;

using AIIDicomObject = Philips.Platform.Common.DicomObject;
using SATAuditTrailToolkit = Philips.Platform.AuditTrail.ServiceAccessToolkit.AuditTrailToolkit;
using SATEventOutcome = Philips.Platform.AuditTrail.ServiceAccessToolkit.EventOutcome;
using SATSecurityAlertType = Philips.Platform.AuditTrail.ServiceAccessToolkit.SecurityAlertType;
using BinarySerializer = Philips.Platform.Dicom.BinarySerializer;
using DeviceConfiguration = Philips.Platform.StorageDevices.DeviceConfiguration;
using DicomDictionary = Philips.Platform.ApplicationIntegration.DataAccess.DicomDictionary;
using DicomObject = Philips.Platform.Dicom.Data.DicomObject;
using DriveType = System.IO.DriveType;
using NativeMethods = Philips.Platform.StorageDevices.NativeMethods;
using PhilipsDictionary = Philips.Platform.Dicom.PhilipsDictionary;
using SDStoreHelper = Philips.Platform.StorageDevices.StoreHelper;
using UnauthorizedAccessException = System.UnauthorizedAccessException;
using UtilitiesTracer = Philips.Platform.Tracing.Tracer;
using Wall = Philips.Platform.Utilities.Wall;
using Philips.Platform.Utilities.ProcessManagement;


namespace PixelDataImplementation
{
    /// <summary>
    /// Implements the dicom object utility methods
    /// </summary>
    internal static class StorageDevicesUtilities
    {
        /// <summary>
        /// Specific tracer for flow tracing
        /// </summary>
        private static readonly UtilitiesTracer flowTracer =
            UtilitiesTracer.CreateTracer(typeof(FlowTracer));

        private static UtilitiesTracer statisticsTracer =
            UtilitiesTracer.CreateTracer(typeof(StatisticsTracer));

        /// <summary>
        /// Flag for DataModelBuffer Cockpit logging
        /// </summary>
        private static bool enableDMBForCockpit;

        /// <summary>
        /// Flag for FastAccessDicomObjectCache Cockpit logging
        /// </summary>
        private static bool enableFADCForCockpit;

        private static string localHostNameOrIpAddress;

        /// <summary>
        /// Specifies whether the control flow tracing is enabled
        /// </summary>
        internal static bool IsFlowTracingEnabled
        {
            get
            {
                return flowTracer.IsVerboseOn;
            }
        }

        /// <summary>
        /// Specifies whether the control flow tracing is enabled
        /// </summary>
        internal static bool IsStatisticsTracingEnabled
        {
            get
            {
                return statisticsTracer.IsVerboseOn;
            }
        }

        /// <summary>
        /// Flag for DataModelBuffer Cockpit logging
        /// </summary>
        internal static bool IsDMBLogForCockpitEnabled
        {
            set
            {
                enableFADCForCockpit = value;
            }
            get
            {
                if (!enableFADCForCockpit)
                {
                    enableFADCForCockpit = Wall.RootGet("TestMode", false);
                }
                return enableFADCForCockpit;
            }
        }

        /// <summary>
        /// Flag for FastAccessDicomObjectCache Cockpit logging
        /// </summary>
        internal static bool IsFADCLogForCockpitEnabled
        {
            set
            {
                enableDMBForCockpit = value;
            }
            get
            {
                if (!enableDMBForCockpit)
                {
                    enableDMBForCockpit = Wall.RootGet("TestMode", false);
                }
                return enableDMBForCockpit;
            }
        }

        private static readonly object syncObject = new object();

        private static readonly Tracer tracer =
            Tracer.CreateTracer(typeof(StorageDevicesUtilities));

        private static readonly int currentProcessId = Process.GetCurrentProcess().Id;

        /// <summary>
        /// Dictionary to keep track of the frame number vs the sop instance UID
        /// Note: This may be a overkill but its temporary solution
        /// </summary>
        private static readonly Dictionary<string, uint> frameNumbersDictionary =
            new Dictionary<string, uint>();

        private const int ModuleId = 070101;
        private const int eventIdInfo = 000002;
        private const int eventIdError = 000001;
        private const int eventIdWarning = 000003;

        private const string Scheduled = "SCHEDULED";

        #region Private Members

        /// <summary>
        /// Creates identifier from string representation of Identifier type. Check
        /// Identifier.ToString() Method for format.
        /// </summary>
        /// <param name="identifierInfo">Identifier Informations in string</param>
        /// <returns>Identifier</returns>
        [SuppressMessage("Microsoft.Globalization",
            "CA1307:SpecifyStringComparison",
            Justification = "todo: needs to be done in many places")]
        internal static Identifier GetIdentifierFrom(string identifierInfo)
        {

            int patientIdKeyWordLength = "PatientId:".Length;
            int patientNameKeyWordLength = "PatientName:".Length;
            int studyInstanceKeyWordLength = "StudyInstanceUID:".Length;
            int seriesInstanceKeyWordLength = "SeriesInstanceUid:".Length;
            int sopInstanceKeyWordLength = "SopInstanceUid:".Length;
            int frameNumberKeywordLength = "FrameNumber:".Length;
            identifierInfo = identifierInfo.Trim();

            int index = identifierInfo.IndexOf("PatientId:");
            // See GetPatientKeyParam(.) method on the details of parsing logic
            string PatientId = identifierInfo.Substring(
                index + patientIdKeyWordLength + 1,
                identifierInfo.IndexOf("PatientName:") - patientIdKeyWordLength - 3
            ).Trim();
            identifierInfo = identifierInfo.Substring(identifierInfo.IndexOf("PatientName:"));

            string PatientName = identifierInfo.Substring(
                patientNameKeyWordLength + 1,
                identifierInfo.IndexOf("IsDummy:") - patientNameKeyWordLength - 3
            ).Trim();

            identifierInfo = identifierInfo.Substring(identifierInfo.IndexOf("StudyInstanceUID:"));

            string StudyInstanceUID = identifierInfo.Substring(
                studyInstanceKeyWordLength + 1,
                identifierInfo.IndexOf("SeriesInstanceUid:") - studyInstanceKeyWordLength - 3
            ).Trim();
            identifierInfo = identifierInfo.Substring(identifierInfo.IndexOf("SeriesInstanceUid:"));

            string SeriesInstanceUID = identifierInfo.Substring(
                seriesInstanceKeyWordLength + 1,
                identifierInfo.IndexOf("SopInstanceUid:") - seriesInstanceKeyWordLength - 3
            ).Trim();

            identifierInfo = identifierInfo.Substring(identifierInfo.IndexOf("SopInstanceUid:"));

            string SopInstanceUid = string.Empty;
            string frame = string.Empty;
            index = identifierInfo.IndexOf("FrameNumber:");
            if (index != -1)
            {
                SopInstanceUid = identifierInfo.Substring(
                    sopInstanceKeyWordLength + 1,
                    identifierInfo.IndexOf("FrameNumber:") - sopInstanceKeyWordLength - 3
                ).Trim();
                identifierInfo = identifierInfo.Substring(identifierInfo.IndexOf("FrameNumber:"));
                frame = identifierInfo.Substring(
                    frameNumberKeywordLength
                ).Trim();
            }
            else
            {
                SopInstanceUid = identifierInfo.Substring(
                    sopInstanceKeyWordLength + 1
                ).Trim();
            }


            uint frameNumber = 0;
            if (!String.IsNullOrEmpty(frame))
            {
                frameNumber = Convert.ToUInt32(frame.Trim(),
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            PatientKey pKey = Identifier.CreatePatientKeyFromDicomObject(
                GetDicomObjectWithPatientIdAndName(PatientId.Trim(), PatientName.Trim()));

            return CreateIdentifier(
                StudyInstanceUID.Trim(),
                SeriesInstanceUID.Trim(),
                SopInstanceUid.Trim(),
                frameNumber,
                pKey);
        }

        /// <summary>
        /// Creates the identifier
        /// </summary>
        /// <param name="studyInstanceUid">study instance UID</param>
        /// <param name="seriesInstanceUid">series instance UID</param>
        /// <param name="sopInstanceUid">SOP instance UID</param>
        /// <param name="frameNumber">Framenumber</param>
        /// <param name="patientKey">The patient key.</param>
        /// <returns>Storage key</returns>
        internal static Identifier CreateIdentifier(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            uint frameNumber,
            PatientKey patientKey
        )
        {
            Identifier tempIdentifier = null;

            if (!string.IsNullOrEmpty(sopInstanceUid))
            {
                tempIdentifier = Identifier.CreateImageIdentifier(
                    patientKey,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid);
            }
            else if (!string.IsNullOrEmpty(seriesInstanceUid))
            {
                tempIdentifier = Identifier.CreateSeriesIdentifier(
                    patientKey,
                    studyInstanceUid,
                    seriesInstanceUid);
            }
            else
            {
                tempIdentifier = Identifier.CreateStudyIdentifier(
                    patientKey,
                    studyInstanceUid);
            }

            if (frameNumber != 0)
            {
                tempIdentifier = Identifier.CreateFrameIdentifier(
                    patientKey,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid, frameNumber);
            }

            return tempIdentifier;
        }


        /// <summary>
        /// Creates a storageKey when a deviceID and a dicomObject is provided.
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="queryLevel">The query level.</param>
        /// <param name="dicomObject">the AIO Dicom object</param>
        /// <param name="parentIdentifier">the Identifier</param>
        /// <returns>
        /// a new instance of a StorageKey
        /// </returns>
        private static StorageKey CreateStorageKey(
            string deviceId,
            QueryLevel queryLevel,
            DicomObject dicomObject,
            Identifier parentIdentifier = null
        )
        {
            string studyID = dicomObject.GetString(DicomDictionary.DicomStudyInstanceUid);
            string seriesID = null;
            string instanceID = null;
            if (queryLevel != QueryLevel.Study)
            {
                seriesID = dicomObject.GetString(DicomDictionary.DicomSeriesInstanceUid);
            }
            if (queryLevel == QueryLevel.Image || queryLevel == QueryLevel.Frame)
            {
                instanceID = dicomObject.GetString(DicomDictionary.DicomSopInstanceUid);
                if (string.IsNullOrEmpty(instanceID))
                {
                    instanceID = dicomObject.GetString(
                        DicomDictionary.DicomReferencedSopInstanceUidInFile);
                    if (!string.IsNullOrEmpty(instanceID))
                    {
                        dicomObject.SetString(
                            DicomDictionary.DicomSopInstanceUid, instanceID);
                    }
                }
            }
            PatientKey pk = null;
            if (parentIdentifier == null)
            {
                pk = Identifier.CreatePatientKeyFromDicomObject(dicomObject);
            }
            else
            {
                pk = parentIdentifier.PatientKey;
            }
            Identifier identifier = null;
            switch (queryLevel)
            {
                case QueryLevel.Frame:
                    //TODO: This code ensure that Frame Identifier is populated
                    //In the query result storage key
                    uint currentFrameNumer;
                    if (!frameNumbersDictionary.TryGetValue(instanceID, out currentFrameNumer))
                    {
                        currentFrameNumer = 1;
                        frameNumbersDictionary.Add(instanceID, currentFrameNumer);
                    }
                    frameNumbersDictionary[instanceID]++;
                    identifier =
                        Identifier.CreateFrameIdentifier(
                        pk,
                        studyID,
                        seriesID,
                        instanceID,
                        currentFrameNumer);
                    break;
                case QueryLevel.Image:
                    identifier = Identifier.CreateImageIdentifier(
                        pk,
                        studyID,
                        seriesID,
                        instanceID);
                    break;
                case QueryLevel.Series:
                    identifier = Identifier.CreateSeriesIdentifier(
                        pk,
                        studyID,
                        seriesID);
                    break;
                case QueryLevel.Study:
                    identifier = Identifier.CreateStudyIdentifier(
                        pk,
                        studyID);
                    break;

                case QueryLevel.Patient:
                    identifier = Identifier.CreatePatientIdentifier(pk);
                    break;
            }
            return new StorageKey(deviceId, identifier);
        }

        /// <summary>
        /// Gets the persistent dicom object.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="queryLevel">The query level.</param>
        /// <param name="dicomObjects">The dicom objects.</param>
        /// <param name="excludeScheduledStudies">
        /// flag indicating that scheduled studies should be excluded
        /// </param>
        /// <param name="parentIdentifier">the Identifier</param>
        /// <returns></returns>
        private static PersistentDicomObjectCollection GetPersistentDicomObjectCollection(
            string deviceId,
            QueryLevel queryLevel,
            IList<DicomObject> dicomObjects,
            bool excludeScheduledStudies = false,
            Identifier parentIdentifier = null
        )
        {
            var storageKeyCollection = new HashSet<StorageKey>();
            var persistentDicomObjectCollection = new PersistentDicomObjectCollection();
            lock (syncObject)
            {
                if (dicomObjects != null)
                {
                    for (int i = 0; i < dicomObjects.Count; i++)
                    {
                        var dicomObject = dicomObjects[i];

                        if (queryLevel == QueryLevel.Study && excludeScheduledStudies)
                        {
                            string studyState = dicomObject.GetString(PhilipsDictionary.StudyState);
                            if (string.Equals(studyState, Scheduled, StringComparison.Ordinal))
                            {
                                continue;
                            }
                        }
                        StorageKey storageKey =
                            CreateStorageKey(
                                deviceId,
                                queryLevel,
                                dicomObject,
                                parentIdentifier);
                        if (!storageKeyCollection.Contains(storageKey))
                        {
                            storageKeyCollection.Add(storageKey);
                            persistentDicomObjectCollection.Add(
                                new PersistentDicomObject(storageKey, dicomObject, null, false));
                        }
                    }
                }
            }
            frameNumbersDictionary.Clear();
            return persistentDicomObjectCollection;
        }
        #endregion



        /// <summary>
        /// Gives Dicom object with passed PatientId and PatientName
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="patientName"></param>
        /// <returns></returns>
        internal static DicomObject GetDicomObjectWithPatientIdAndName(
            string patientId, string patientName)
        {
            DicomObject dicomObject = new DicomObject();
            dicomObject.SetString(DicomDictionary.DicomPatientId, patientId);
            dicomObject.SetString(DicomDictionary.DicomPatientName, patientName);
            return dicomObject;
        }

        /// <summary>
        /// Checks the expected state against current state.
        /// </summary>
        /// <param name="currentState">current state</param>
        /// <param name="expectedState">expected state</param>
        /// <returns>is expected state found</returns>
        internal static bool IsExpectedState(string currentState, string expectedState)
        {
            return
                !string.IsNullOrEmpty(currentState) &&
                currentState.ToUpperInvariant() == expectedState;
        }

        /// <summary>
        ///  Checks sum of skipped and archived/completed states counter is equal to total number of series.
        /// </summary>
        internal static bool CheckArchivedState(
            int actualseriesCount, int skippedSeriesCount, int archivedSeriesCount)
        {
            int totalCount = skippedSeriesCount + archivedSeriesCount;
            if (actualseriesCount == totalCount)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the current process Id
        /// </summary>
        internal static int CurrentProcessId
        {
            get
            {
                return currentProcessId;
            }
        }

        /// <summary>
        /// Default Pixel Data Tag
        /// </summary>
        internal static DictionaryTag PixelDataAsBytesTag =
            new DictionaryTag(
                DicomDictionary.DicomPixelData.Tag,
                DicomVR.OB,
                DicomDictionary.DicomPixelData.ValueMultiplicity,
                DicomDictionary.DicomPixelData.Name,
                DicomDictionary.DicomPixelData.ImplementerId);

        internal static DictionaryTag GetPixelDataTag(DicomVR dicomTagVr)
        {
            var dicomVr = dicomTagVr == DicomVR.None ? DicomVR.OB : dicomTagVr;
            var tag = new DictionaryTag(
                DicomDictionary.DicomPixelData.Tag,
                dicomVr,
                DicomDictionary.DicomPixelData.ValueMultiplicity,
                DicomDictionary.DicomPixelData.Name,
                DicomDictionary.DicomPixelData.ImplementerId);
            return tag;
        }

        /// <summary>
        /// Updates the study status in the storage device.
        /// </summary>
        /// <param name="studyKey">
        /// The studykey whose status needs to be updated.
        /// </param>
        /// <param name="studyStatus">
        /// The updated status.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="studyKey"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="studyKey"/> does not point to a valid study.
        /// </exception>
        /// <exception cref="Philips.Platform.Common.DataAccess.DeviceException">
        /// Storage device details cannot be retrieved for the device where the 
        /// <paramref name="studyKey"/> is stored.
        /// </exception>
        /// <remarks>
        /// If the 
        /// </remarks>
        public static void UpdateStudyStatusInDatabase(StorageKey studyKey, string studyStatus)
        {
            DictionaryTag tag = PhilipsDictionary.StudyState;
            KeyValuePair<DictionaryTag, object> statusTagValuePair =
                new KeyValuePair<DictionaryTag, object>(tag, studyStatus);
            UpdateStudyStatusInDatabase(studyKey, statusTagValuePair);
        }

        /// <summary>
        /// Updates the study status in the storage device.
        /// <remarks>It updates only the columns in study table and does not modify
        /// the study header cdo files.</remarks>
        /// </summary>
        /// <param name="studyKey">
        /// The studykey whose status needs to be updated.
        /// </param>
        /// <param name="statusTagValuePair">
        /// Prefetch Status flags
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="studyKey"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="studyKey"/> does not point to a valid study.
        /// </exception>
        /// <exception cref="Philips.Platform.Common.DataAccess.DeviceException">
        /// Storage device details cannot be retrieved for the device where the 
        /// <paramref name="studyKey"/> is stored.
        /// </exception>
        /// <remarks>
        /// If the 
        /// </remarks>
        public static void UpdateStudyStatusInDatabase(
            StorageKey studyKey,
            KeyValuePair<DictionaryTag, object>? statusTagValuePair
        )
        {
            if (studyKey == null)
            {
                throw new ArgumentNullException("studyKey");
            }
            if (String.IsNullOrWhiteSpace(studyKey.Identifier.StudyInstanceUid))
            {
                throw new ArgumentException(
                    "The study key does not point to a valid study.", "studyKey");
            }

            string deviceId = studyKey.SourceDevice;
            StorageDeviceBase storageDevice = StorageDevicesManager.GetDatabaseStoreBase(deviceId);

            if (storageDevice == null)
            {
                throw new Common.DataAccess.DeviceException(
                    "Storage device details for the device " + deviceId +
                    "could not be retrieved.");
            }

            //pack the modified attributes in a DicomObject
            DicomObject modifiedAttributes = new DicomObject();
            if (statusTagValuePair != null)
            {
                modifiedAttributes.SetValue(
                    statusTagValuePair.Value.Key,
                    statusTagValuePair.Value.Value
                );
            }

            // Sets the study accessed date-time
            modifiedAttributes.SetDateTime(DicomDictionary.DicomDateTime, DateTime.Now);

            // re-construct study key to make sure that
            // passed key is not series or image key 
            studyKey = new StorageKey(studyKey.SourceDevice,
                Identifier.CreateStudyIdentifier(
                studyKey.Identifier.PatientKey,
                studyKey.Identifier.StudyInstanceUid));
            //Below log is added for PiimImportStudyStatus tag update.
            if (modifiedAttributes.HasValue(PhilipsDictionary.PiimImportStudyStatus))
            {
                tracer.TraceInfo("Started updating import status tag in database with value "
                    + modifiedAttributes.GetString(PhilipsDictionary.PiimImportStudyStatus)
                    + " for study " + studyKey.Identifier.StudyInstanceUid.Anonymize());
            }

            //If data is already in-memory and has not been flushed to DB,
            //we will update it and return otherwise update in normal flow.
            if (DataModelBuffer.TryUpdateStudyAndRaiseEvent(studyKey, modifiedAttributes, deviceId))
            {
                return;
            }

            try
            {
                //update the details in db.
                UpdateStudyInDatabase(
                    studyKey.SourceDevice, studyKey.Identifier, modifiedAttributes);
            }
            catch (EndOfStreamException ex)
            {
                string error = "The study " + studyKey.Anonymize() + " does not exist.";
                tracer.TraceInfo("Error while updating study status : " + error);
                throw new ObjectNotPresentException(error, ex);
            }
        }

        /// <summary>
        /// Updates the study date-time in the database.
        /// </summary>
        /// <param name="deviceId">
        /// The device id.
        /// </param>
        /// <param name="identifier">
        /// The study identifier.
        /// </param>
        /// <param name="dateTime">
        /// The modified date-time.
        /// </param>
        internal static void UpdateStudyDateTimeInDatabase(
            string deviceId,
            Identifier identifier,
            DateTime dateTime
        )
        {
            string patientKey = GetPatientUidForPatient(identifier.PatientKey);
            var dbStore = DatabaseStoreBase.For(deviceId);
            dbStore.UpdateColumnByDictionaryTag(
                Level.Study, identifier.StudyInstanceUid,
                DicomDictionary.DicomDateTime, dateTime, patientKey);
        }

        /// <summary>
        /// Updates the Date-Time of Study/Series store in the database.
        /// </summary>
        /// <param name="deviceId">
        /// The device id.
        /// </param>
        /// <param name="identifier">
        /// Identifier.
        /// </param>
        /// <param name="dateTime">
        /// The Store date-time.
        /// </param>
        /// <param name="level">level</param>
        internal static void UpdateStoreTimeInDatabase(
            string deviceId,
            Identifier identifier,
            DateTime dateTime,
            Level level
        )
        {
            var dbStore = DatabaseStoreBase.For(deviceId);
            if (level == Level.Study)
            {
                string patientKey = GetPatientUidForPatient(identifier.PatientKey);
                dbStore.UpdateColumnByDictionaryTag(
                    level, identifier.StudyInstanceUid,
                    PhilipsDictionary.PiimDicomStoreDateTime, dateTime, patientKey);
            }
            else if (level == Level.Series)
            {
                dbStore.UpdateColumnByDictionaryTag(
                    level, identifier.SeriesInstanceUid,
                    PhilipsDictionary.PiimDicomStoreDateTime,
                    dateTime,
                    identifier.StudyInstanceUid);
            }
        }

        /// <summary>
        /// Updates the study in the database.
        /// </summary>
        /// <param name="deviceId">
        /// The device id. (what else??)
        /// </param>
        /// <param name="identifier">
        /// The study identifier.
        /// </param>
        /// <param name="delta">
        /// The modified attributes.
        /// </param>
        internal static void UpdateStudyInDatabase(
            string deviceId,
            Identifier identifier,
            DicomObject delta)
        {
            var dbStore = DatabaseStoreBase.For(deviceId);
            string studyInstanceUid = identifier.StudyInstanceUid;
            var currentFastAccessDicomObject = dbStore.GetStorables(Level.Study, studyInstanceUid);
            var currentIdentifier = GetUpdatedStudyIdentifierIfPatientKeyIsDummy();
            GetPatientStudyRowContent(out var patientRowContent, out var studyRowContent);
            string currentPatientUid = dbStore.GetParent(Level.Study, studyInstanceUid);
            string toBeUpdatedPatientUid = GetToBeUpdatedPatientUid(out PatientKey newPatientKey);
            UpdateCurrentStudyRow();
            if (IsPatientIdentificationChanged(currentPatientUid, toBeUpdatedPatientUid))
            {
                tracer.TraceInfo(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Patient identification information changed from {0} to {1} ",
                        currentPatientUid.Anonymize(),
                        toBeUpdatedPatientUid.Anonymize()));
                long newParOid = CreateOrUpdateRowForNewPatient();
                UpdateStudyToPointNewPatientRecord(newParOid);
                if (CheckIfCurrentPatientCanBeRemoved())
                {
                    RemoveCurrentPatient();
                }
            }
            else
            {
                UpdateCurrentPatientRow();
            }
            TracePiimImportStudyStatus();
            PublishStudyModificationEvent();

            void GetPatientStudyRowContent(out RowContent patientRow, out RowContent studyRow)
            {
                DicomObject modifiedObjectDetails = new DicomObject();
                modifiedObjectDetails.Combine(delta);
                if (!modifiedObjectDetails.HasTag(DicomDictionary.DicomStudyInstanceUid))
                {
                    modifiedObjectDetails.SetString(DicomDictionary.DicomStudyInstanceUid, studyInstanceUid);
                }
                Dictionary<Level, DicomLevelInfo> dicomInfoFor = SDStoreHelper.GetLevelToDicomInfoMap(deviceId, delta);
                patientRow = RowContent.GetPatientRowContent(modifiedObjectDetails, dicomInfoFor[Level.Patient]);
                studyRow = RowContent.GetOrCreateStudyRowContent(modifiedObjectDetails, dicomInfoFor[Level.Study]);
            }

            string GetToBeUpdatedPatientUid(out PatientKey patientKey)
            {
                DicomObject modifiedObjectDetails = new DicomObject();
                if (currentFastAccessDicomObject != null)
                {
                    modifiedObjectDetails.Combine(currentFastAccessDicomObject);
                }
                modifiedObjectDetails.Combine(delta);
                patientKey = FileStoreUtilities.CreatePatientKey(modifiedObjectDetails);
                return GetPatientUidForPatient(patientKey);
            }

            bool IsPatientIdentificationChanged(string existingPatientUid, string modifiedPatientUid)
            {
                return !string.Equals(existingPatientUid, modifiedPatientUid, StringComparison.Ordinal);
            }

            long CreateOrUpdateRowForNewPatient()
            {
                long newParOid;
                if (dbStore.RowExists(Level.Patient, toBeUpdatedPatientUid, null))
                {
                    dbStore.UpdateRow(
                        Level.Patient,
                        toBeUpdatedPatientUid,
                        "",
                        delta,
                        patientRowContent,
                        out newParOid);
                }
                else
                {
                    RowRecord newPatientRowRecord = new RowRecord(
                        deviceId,
                        patientRowContent,
                        Level.Patient,
                        toBeUpdatedPatientUid,
                        null,
                        newPatientKey);
                    tracer.TraceInfo("Creating new patient row with " + toBeUpdatedPatientUid.Anonymize());
                    try
                    {
                        dbStore.InsertRow(newPatientRowRecord, out newParOid);
                    }
                    catch (ConstraintException ex)
                    {
                        if (ex.InnerException == null ||
                            ((DbException)ex.InnerException).ErrorCode != Convert.ToInt32(SQLiteErrorCode.Constraint))
                        {
                            tracer.TraceInfo(
                                "CreateOrUpdateRowForNewPatient failed. Call stack: "
                                + new System.Diagnostics.StackTrace(), ex);
                            throw;
                        }
                        if (!dbStore.RowExists(Level.Patient, toBeUpdatedPatientUid, null))
                        {
                            tracer.TraceInfo(
                                "CreateOrUpdateRowForNewPatient failed even when row does not exist. Call stack: "
                                + new System.Diagnostics.StackTrace(), ex);
                            throw;
                        }
                        dbStore.UpdateRow(
                            Level.Patient,
                            toBeUpdatedPatientUid,
                            "",
                            delta,
                            patientRowContent,
                            out newParOid);
                    }
                }
                return newParOid;
            }

            void UpdateStudyToPointNewPatientRecord(long newParOid)
            {
                UpdateColumnInDatabase(
                    Level.Study,
                    deviceId,
                    studyInstanceUid,
                    StorageDevices.Constants.ParUidColumn,
                    toBeUpdatedPatientUid,
                    toBeUpdatedPatientUid);
                UpdateColumnInDatabase(
                    Level.Study,
                    deviceId,
                    studyInstanceUid,
                    StorageDevices.Constants.ParOidColumn,
                    newParOid,
                    toBeUpdatedPatientUid);
            }

            DicomObject GetToBeUpdatedDicomObject()
            {
                DicomObject currentState;
                if (currentFastAccessDicomObject != null)
                {
                    currentState = currentFastAccessDicomObject.ShallowCopy() as DicomObject;
                    if (currentState != null)
                    {
                        currentState.Combine(delta);
                    }
                }
                else
                {
                    currentState = new DicomObject();
                    currentState.Combine(delta);
                }
                return currentState;
            }

            void TracePiimImportStudyStatus()
            {
                if (delta.HasValue(PhilipsDictionary.PiimImportStudyStatus))
                {
                    tracer.TraceInfo(
                        "Updated import status tag and raised event with value " +
                        delta.GetString(PhilipsDictionary.PiimImportStudyStatus) +
                        " for study " +
                        studyInstanceUid.Anonymize());
                }
            }

            //TICS -COV_CS_RESOURCE_LEAK_02
            void PublishStudyModificationEvent()
            {
                var toBeUpdatedDicomObject = GetToBeUpdatedDicomObject();
                StorageDeviceEventArgs storageDeviceEventArgs = new StorageDeviceEventArgs(
                    EventTypes.Modify,
                    EventLevels.Study,
                    new StorageKey(dbStore.DeviceId, currentIdentifier),
                    DateTime.Now,
                    currentFastAccessDicomObject?.ShallowCopy(),
                    toBeUpdatedDicomObject);
                DBEventPublisher.Publish(
                    dbStore.DeviceId + EventLevels.Study + EventTypes.Modify,
                    storageDeviceEventArgs);
            }
            //TICS +COV_CS_RESOURCE_LEAK_02

            bool CheckIfCurrentPatientCanBeRemoved()
            {
                return 0 ==
                    dbStore.GetNumberOfMatchingRecords(
                        Level.Study,
                        Identifier.CreatePatientIdentifier(currentIdentifier.PatientKey),
                        new QueryFilter());
            }

            void RemoveCurrentPatient()
            {
                tracer.TraceInfo(
                    "Deleting the patient with " + currentPatientUid.Anonymize());
                dbStore.DeleteRow(
                    Level.Patient,
                    dbStore.GetObjectOid(Level.Patient, currentPatientUid, null),
                    out _,
                    false);
            }

            Identifier GetUpdatedStudyIdentifierIfPatientKeyIsDummy()
            {
                currentIdentifier = identifier;
                if (identifier.PatientKey.IsDummy && currentFastAccessDicomObject != null)
                {
                    currentIdentifier = Identifier.CreateStudyIdentifier(
                        Identifier.CreatePatientKeyFromDicomObject(currentFastAccessDicomObject),
                        studyInstanceUid);
                }
                return currentIdentifier;
            }

            void UpdateCurrentStudyRow()
            {
                dbStore.UpdateRow(Level.Study, studyInstanceUid, currentPatientUid, delta, studyRowContent, out _);
            }

            void UpdateCurrentPatientRow()
            {
                dbStore.UpdateRow(Level.Patient, currentPatientUid, "", delta, patientRowContent, out _);
            }
        }

        /// <summary>
        /// Updates the study in the database.
        /// </summary>
        /// <param name="txnHandler"></param>
        /// <param name="deviceId"></param>
        /// <param name="identifier">
        /// The study identifier.
        /// </param>
        /// <param name="delta">
        /// The modified attributes.
        /// </param>
        /// <param name="oldPatientInfo"></param>
        /// <param name="studyPath"></param>
        /// <param name="reconciliationStatusFlag"></param>
        internal static void UpdateStudyInDatabaseInTxn(
            TransactionHandler txnHandler,
            string deviceId,
            Identifier identifier,
            DicomObject delta, DicomObject oldPatientInfo,
            string studyPath,
            ReconciliationStatusFlag reconciliationStatusFlag
        )
        {
            string studyInstanceUid = identifier.StudyInstanceUid;
            Dictionary<Level, DicomLevelInfo> dicomInfoFor =
                SDStoreHelper.GetLevelToDicomInfoMap(deviceId, delta);

            //To get row details, we also need the study instance uid.
            DicomObject modifiedObjectDetails = new DicomObject();
            modifiedObjectDetails.Combine(delta);
            if (!modifiedObjectDetails.HasTag(DicomDictionary.DicomStudyInstanceUid))
            {
                modifiedObjectDetails.SetString(
                    DicomDictionary.DicomStudyInstanceUid,
                    studyInstanceUid);
            }
            var patientRowContent = RowContent.GetPatientRowContent(
                modifiedObjectDetails, dicomInfoFor[Level.Patient]);

            var studyRowContent = RowContent.GetOrCreateStudyRowContentEx(txnHandler,
                modifiedObjectDetails, dicomInfoFor[Level.Study], studyPath);

            var dbStore = DatabaseStoreBase.CreateOnly(deviceId);
            string patientkey = GetPatientUidForPatient(identifier.PatientKey);
            string parentUid = patientkey;
            // Any information that is NULL should be removed from patient 
            if (!delta.HasValue(DicomDictionary.DicomPatientBirthDate))
            {
                delta.SetNoValue(DicomDictionary.DicomPatientBirthDate);
            }
            if (!delta.HasValue(DicomDictionary.DicomPatientSex))
            {
                delta.SetString(DicomDictionary.DicomPatientSex, string.Empty);
            }

            //The new patient uid should be updated in both the patient and study tables.
            //Create the uid from modified attributes
            string newPatientUid =
                GetPatientUidForPatient(FileStoreUtilities.CreatePatientKey(delta));
            dbStore.UpdateRowInTxn(txnHandler, deviceId, Level.Patient, parentUid, newPatientUid,
                "", delta, patientRowContent, reconciliationStatusFlag);
            string uid = studyInstanceUid;
            DicomObject previousState = dbStore.GetStudyStorable(txnHandler, uid, parentUid);

            dbStore.UpdateRowInTxn(txnHandler, deviceId,
                Level.Study,
                uid,
                "",
                newPatientUid,
                delta,
                studyRowContent,
                reconciliationStatusFlag);
            dbStore.UpdateColumnInTxn(txnHandler, deviceId,
                Level.Study,
                uid,
                StorageDevices.Constants.PreviousPatientId,
                oldPatientInfo.GetString(DicomDictionary.DicomPatientId),
                newPatientUid
                );
            dbStore.UpdateColumnInTxn(txnHandler, deviceId,
                Level.Study,
                uid,
                StorageDevices.Constants.PreviousPatientName,
                oldPatientInfo.GetString(DicomDictionary.DicomPatientName),
                newPatientUid
                );

            //Publish Study modified Event
            DicomObject currentState;
            if (previousState != null)
            {
                currentState = previousState.ShallowCopy() as DicomObject;
                if (currentState != null)
                {
                    currentState.Combine(delta);
                }
            }
            else
            {
                currentState = new DicomObject();
                currentState.Combine(delta);
            }

            //TICS -COV_CS_RESOURCE_LEAK_02
            StorageDeviceEventArgs storageDeviceEventArgs =
                new StorageDeviceEventArgs(
                    EventTypes.Modify,
                    EventLevels.Study,
                    new StorageKey(dbStore.DeviceId, identifier),
                    DateTime.Now,
                    previousState != null ? previousState.ShallowCopy() : null,
                    currentState
                );
            DBEventPublisher.Publish(
                dbStore.DeviceId + EventLevels.Study + EventTypes.Modify,
                storageDeviceEventArgs);
        }
        //TICS +COV_CS_RESOURCE_LEAK_02

        ///<summary>
        /// Updates the series in the database.
        /// </summary>
        /// <param name="txnHandler"></param>
        /// <param name="deviceId">
        /// The device id. (what else??)
        /// </param>
        /// <param name="identifier">
        /// The series identifier.
        /// </param>
        /// <param name="delta">
        /// The modified attributes.
        /// </param>
        internal static void UpdateSeriesInDatabaseInTxn(
            TransactionHandler txnHandler,
            string deviceId,
            Identifier identifier,
            DicomObject delta
        )
        {
            string seriesInstanceUid = identifier.SeriesInstanceUid;

            //To get row details, we also need the series instance uid.
            DicomObject modifiedObjectDetails = new DicomObject();
            modifiedObjectDetails.Combine(delta);
            if (!modifiedObjectDetails.HasTag(DicomDictionary.DicomSeriesInstanceUid))
            {
                modifiedObjectDetails.SetString(
                    DicomDictionary.DicomSeriesInstanceUid,
                    seriesInstanceUid);
            }

            Dictionary<Level, DicomLevelInfo> dicomInfoFor =
                SDStoreHelper.GetLevelToDicomInfoMap(deviceId, delta);

            var seriesRowContent = RowContent.GetSeriesRowContent(
                modifiedObjectDetails, dicomInfoFor[Level.Series]);

            var dbStore = DatabaseStoreBase.For(deviceId);
            string patientKey = GetPatientUidForPatient(identifier.PatientKey);

            if (identifier.PatientKey.IsDummy)
            {
                patientKey = dbStore.GetParent(Level.Series, seriesInstanceUid);
            }
            string parentId = identifier.StudyInstanceUid;

            dbStore.UpdateRowInTxn(txnHandler,
                deviceId, Level.Series, seriesInstanceUid, patientKey, parentId,
                delta, seriesRowContent, ReconciliationStatusFlag.None);
        }

        ///<summary>
        /// Updates the series in the database.
        /// </summary>
        /// <param name="deviceId">
        /// The device id. (what else??)
        /// </param>
        /// <param name="identifier">
        /// The series identifier.
        /// </param>
        /// <param name="delta">
        /// The modified attributes.
        /// </param>
        internal static void UpdateSeriesInDatabase(
            string deviceId,
            Identifier identifier,
            DicomObject delta
        )
        {
            string seriesInstanceUid = identifier.SeriesInstanceUid;
            string studyInstanceUid = identifier.StudyInstanceUid;

            //To get row details, we also need the series instance uid and study instance UID.
            DicomObject modifiedObjectDetails = new DicomObject();
            modifiedObjectDetails.Combine(delta);
            if (!modifiedObjectDetails.HasTag(DicomDictionary.DicomSeriesInstanceUid))
            {
                modifiedObjectDetails.SetString(
                    DicomDictionary.DicomSeriesInstanceUid,
                    seriesInstanceUid);
            }

            if (!modifiedObjectDetails.HasTag(DicomDictionary.DicomStudyInstanceUid))
            {
                modifiedObjectDetails.SetString(
                    DicomDictionary.DicomStudyInstanceUid,
                    studyInstanceUid);
            }

            Dictionary<Level, DicomLevelInfo> dicomInfoFor =
                SDStoreHelper.GetLevelToDicomInfoMap(deviceId, delta);

            var seriesRowContent = RowContent.GetSeriesRowContent(
                modifiedObjectDetails, dicomInfoFor[Level.Series]);

            long objectOid;
            var dbStore = DatabaseStoreBase.For(deviceId);
            dbStore.UpdateRow(Level.Series, seriesInstanceUid, studyInstanceUid,
                delta, seriesRowContent, out objectOid);
        }

        /// <summary>
        /// Gets the AIP dicom object.
        /// </summary>
        /// <param name="abstractDicomObject">The abstract dicom object.</param>
        /// <returns></returns>
        internal static DicomObject GetAipDicomObject(AIIDicomObject abstractDicomObject)
        {
            if (abstractDicomObject == null)
            {
                return new DicomObject();
            }

            DicomObject aipDicomObject = abstractDicomObject as DicomObject;
            if (aipDicomObject == null)
            {
                aipDicomObject = new DicomObject();
                aipDicomObject.Combine(abstractDicomObject);
            }
            return aipDicomObject;
        }

        /// <summary>
        /// Extracts the DicomObject from the given QueryResult and builds a list of
        /// DicomObjects
        /// </summary>
        /// <param name="result">the query result</param>
        /// <param name="level">the query level</param>
        /// <returns>list of dicom objects</returns>
        public static IList<DicomObject> ExtractDicomObjects(QueryResult result, QueryLevel level)
        {
            if (result != null)
            {
                if (result.QueryResultStream != null)
                {
                    IList<DicomObject> queryResult = ExtractDicomObjects(result.QueryResultStream);
                    if (result.QueryResultList != null && result.QueryResultList.Count > 0)
                    {
                        queryResult =
                            FileStoreUtilities.UnionOf(result.QueryResultList, queryResult, level);
                    }
                    return queryResult;
                }
                else
                {
                    return result.QueryResultList;
                }
            }

            // FDG: do not return null for collections.
            return new List<DicomObject>(0);
        }

        /// <summary>
        /// Extracts the DicomObject from the given QueryResult and builds a list of
        /// DicomObjects
        /// </summary>
        public static IList<DicomObject> ExtractDicomObjectsWithFilter(
            QueryResult result,
            QueryLevel level,
            QueryFilter filter
        )
        {
            if (result != null)
            {
                if (result.QueryResultStream != null)
                {
                    IList<DicomObject> queryResult =
                        ExtractDicomObjectsWithFilter(result.QueryResultStream, filter);
                    if (result.QueryResultList != null && result.QueryResultList.Count > 0)
                    {
                        queryResult =
                            FileStoreUtilities.UnionOf(result.QueryResultList, queryResult, level);
                    }
                    return queryResult;
                }
                else
                {
                    return result.QueryResultList;
                }
            }

            // FDG: do not return null for collections.
            return new List<DicomObject>(0);
        }

        /// <summary>
        /// Extracts the DicomObject from the given QueryResult and builds a list of
        /// DicomObjects
        /// </summary>
        /// <param name="result">the query result</param>
        /// <returns>list of dicom objects</returns>
        public static IList<DicomObject> ExtractDicomObjects(QueryResult result)
        {
            if (result != null)
            {
                if (result.QueryResultStream != null)
                {
                    List<DicomObject> queryResult = ExtractDicomObjects(result.QueryResultStream);
                    if (result.QueryResultList != null && result.QueryResultList.Count > 0)
                    {
                        queryResult.InsertRange(0, result.QueryResultList);
                    }
                    return queryResult;
                }
                else
                {
                    return result.QueryResultList;
                }
            }
            else
            {
                // FDG: do not return null for collections ..
                return new List<DicomObject>(0);
            }
        }

        /// <summary>
        /// Extracts the DicomOject from the given query result and the device ID. This method
        /// builds a list of PersistenDicomObjects
        /// </summary>
        /// <param name="deviceId">the deviceID</param>
        /// <param name="queryLevel">The query level.</param>
        /// <param name="result">the query result</param>
        /// <param name="excludeScheduledStudies">
        /// flag indicating that scheduled studies should be excluded
        /// </param>
        /// <param name="parentIdentifier">the Identifier</param>
        /// <returns>
        /// A Collection of PersistentDicomObjects
        /// </returns>
        public static PersistentDicomObjectCollection ExtractResultsAsPDO(
            string deviceId,
            QueryLevel queryLevel,
            QueryResult result,
            bool excludeScheduledStudies = false,
            Identifier parentIdentifier = null
        )
        {
            if (result == null)
            {
                // return empty collection if no results
                return new PersistentDicomObjectCollection();
            }
            if (result.QueryResultsWithDeviceId != null)
            {
                var pdoCollection = new PersistentDicomObjectCollection();
                foreach (var queryResult in result.QueryResultsWithDeviceId)
                {
                    pdoCollection.AddRange(GetPersistentDicomObjectCollection(
                        queryResult.Key,
                        queryLevel,
                        queryResult.Value,
                        excludeScheduledStudies,
                        parentIdentifier));
                }
                return pdoCollection;
            }
            IList<DicomObject> dicomObjects = ExtractDicomObjects(result);

            return GetPersistentDicomObjectCollection(
                deviceId,
                queryLevel,
                dicomObjects,
                excludeScheduledStudies,
                parentIdentifier);
        }

        /// <summary>
        /// Extracts the results as PDO.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="queryLevel">The query level.</param>
        /// <param name="queryResultSteam">The query result steam.</param>
        /// <returns></returns>
        public static PersistentDicomObjectCollection ExtractResultsAsPDO(
            string deviceId,
            QueryLevel queryLevel,
            Stream queryResultSteam
        )
        {
            List<DicomObject> dicomObjects = ExtractDicomObjects(queryResultSteam);
            return GetPersistentDicomObjectCollection(deviceId, queryLevel, dicomObjects);
        }

        /// <summary>
        /// Extracts the DicomObject from the given stream for Patient Dicom Objects.
        /// </summary>
        /// <param name="dicomObjectStream">the dicom object stream</param>
        /// <returns>
        /// List of dicom objects
        /// </returns>
        public static List<DicomObject> ExtractDicomObjects(Stream dicomObjectStream)
        {
            List<DicomObject> dicomObjects = new List<DicomObject>();
            if (dicomObjectStream != null)
            {
                dicomObjectStream.Position = 0;
                using (BinaryReader binaryReader = new BinaryReader(dicomObjectStream))
                {
                    bool canSeek = dicomObjectStream.CanSeek;
                    while (true)
                    {
                        // Avoid ALWAYS getting an EndOfStreamException if the 
                        //stream supports seeking.
                        if (canSeek && dicomObjectStream.Position == dicomObjectStream.Length)
                        {
                            break;
                        }
                        try
                        {
                            dicomObjects.Add(BinarySerializer.Load(binaryReader));
                        }
                        catch (EndOfStreamException)
                        {
                            //ignore exception. Reached the end of the file, stop reading it.
                            break;
                        }
                    }
                }
            }
            return dicomObjects;
        }

        /// <summary>
        /// Extracts the DicomObject from the given stream for Patient Dicom Objects.
        /// </summary>
        public static List<DicomObject> ExtractDicomObjectsWithFilter(
            Stream dicomObjectStream,
            QueryFilter filter
        )
        {
            List<DicomObject> dicomObjects = new List<DicomObject>();
            using (BinaryReader binaryReader = new BinaryReader(dicomObjectStream))
            {
                bool canSeek = dicomObjectStream.CanSeek;
                while (true)
                {
                    // Avoid ALWAYS getting an EndOfStreamException if the stream supports seeking.
                    if (canSeek && dicomObjectStream.Position == dicomObjectStream.Length)
                    {
                        break;
                    }
                    try
                    {
                        DicomObject dicomObject = BinarySerializer.Load(binaryReader);
                        if (FilterMatcher.SatisfiesFilter(dicomObject, filter))
                        {
                            dicomObjects.Add(dicomObject);
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        //ignore exception. Reached the end of the file, stop reading it.
                        break;
                    }
                }
            }
            return dicomObjects;
        }

        /// <summary>
        /// Extracts the dicom object.
        /// </summary>
        /// <param name="dicomObjectBytes">The dicom object bytes.</param>
        /// <returns>
        /// Binary deserialized Dicom object
        /// </returns>
        public static DicomObject GetDicomObjectFromByteArray(byte[] dicomObjectBytes)
        {
            DicomObject dicomObject = null;
            if (dicomObjectBytes != null && dicomObjectBytes.Length > 0)
            {
                // TODO: Use recyclable reader with associated stream
                using (var memoryStream = new MemoryStream(dicomObjectBytes))
                {
                    using (var reader = new BinaryReader(memoryStream))
                    {
                        dicomObject = BinarySerializer.Load(reader);
                    }
                }
            }
            return dicomObject;
        }

        /// <summary>
        /// Gets the dicom object as byte array.
        /// </summary>
        /// <param name="dicomObject">The dicom object.</param>
        /// <returns>
        /// Binary serialized DicomObject
        /// </returns>
        public static byte[] GetDicomObjectAsByteArray(DicomObject dicomObject)
        {
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    BinarySerializer.Store(writer, dicomObject);
                }
                bytes = memoryStream.ToArray();
            }
            return bytes;
        }

        /// <summary>
        /// Calculates the size of the pixelData from the dicomObject attributes.
        /// </summary>
        /// <param name="dicomObject">The dicom object.</param>
        /// <returns></returns>
        public static int GetSizeOfPixelData(AIIDicomObject dicomObject)
        {
            if (
                !dicomObject.HasTag(DicomDictionary.DicomRows) ||
                !dicomObject.HasTag(DicomDictionary.DicomColumns))
            {
                return 0;
            }

            ushort rows = dicomObject.GetUInt16(DicomDictionary.DicomRows).Value;
            ushort columns = dicomObject.GetUInt16(DicomDictionary.DicomColumns).Value;

            if (
                !dicomObject.HasTag(DicomDictionary.DicomSamplesPerPixel) ||
                !dicomObject.HasTag(DicomDictionary.DicomBitsAllocated))
            {
                return 0;
            }

            //for YBR_Full, samples_per_pixel is always 2.
            ushort samplePerPixel;
            if (
                dicomObject.GetString(DicomDictionary.DicomPhotometricInterpretation) !=
                    PhotometricInterpretation.YbrFull422
            )
            {
                samplePerPixel = dicomObject.GetUInt16(DicomDictionary.DicomSamplesPerPixel).Value;
            }
            else
            {
                samplePerPixel = 2;
            }

            ushort bitsAllocated = dicomObject.GetUInt16(DicomDictionary.DicomBitsAllocated).Value;
            int size = rows * columns * ((bitsAllocated + 7) / 8) * samplePerPixel;
            if (size == 0)
            {
                const string errormessage = "Could not decipher the size of the pixelData. ";
                LogMessage(errormessage);
                throw new ArgumentException(errormessage);
            }
            return size;
        }

        /// <summary>
        /// Gets the performed procedure step based on ppsId
        /// </summary>
        /// <param name="ppsId"></param>
        /// <returns>performed procedure step</returns>
        internal static DicomObject GetPerformedProcedureStep(string ppsId)
        {
            Exception exception = null;
            DicomObject ppsObject = null;
#pragma warning disable PFB4327 // Log caught exceptions.
            try
            {
                string deviceId = GetPrimaryDatabaseDeviceId();
                var dbStore = DatabaseStoreBase.For(deviceId);
                ppsObject = dbStore.GetPerformedProcedureStep(ppsId);
            }
            catch (SqlException ex)
            {
                exception = ex;
            }
            catch (DbException ex)
            {
                exception = ex;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
#pragma warning restore PFB4327 // Log caught exceptions.
            if (exception != null)
            {
                var message =
                    "Error in getting PerformedProcedureSteps details.ppsId -" +
                    ppsId.Anonymize();
                StorageDevicesUtilities.LogError(message, exception);
            }

            return ppsObject;
        }

        /// <summary>
        /// Update the performed procedure step stored under study with the ppsId mentioned
        /// </summary>
        /// <param name="modifiedPpsObject"></param>
        /// <param name="ppsId"></param>
        internal static void UpdatePerformedProcedureStep(
            DicomObject modifiedPpsObject,
            string ppsId
        )
        {
#pragma warning disable PFB4327 // Log caught exceptions.
            Exception exception = null;
            try
            {
                string deviceId = GetPrimaryDatabaseDeviceId();
                var dbStore = DatabaseStoreBase.For(deviceId);
                dbStore.UpdateMpps(modifiedPpsObject, ppsId);
            }
            catch (SqlException ex)
            {
                exception = ex;
            }
            catch (DbException ex)
            {
                exception = ex;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
#pragma warning restore PFB4327 // Log caught exceptions.
            if (exception != null)
            {
                var message = "Error in updating MPPS.ppsId -" + ppsId.Anonymize();
                StorageDevicesUtilities.LogError(message, exception);
            }
        }

        /// <summary>
        /// Check if multiple pps object per study are supported
        /// </summary>
        /// <returns>returns true if multiple pps object per study are supported</returns>
        internal static bool IsMultiplePpsPerStudySupported()
        {
            return ConfigurationManager.Instance.GlobalConfiguration.EnableMultiplePpsPerStudy;
        }

        private static string GetPrimaryDatabaseDeviceId()
        {
            return ConfigurationManager.Instance.GetPrimaryDatabaseDeviceId();
        }

        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="message">The message.</param>
        internal static void LogMessage(string message)
        {
            //tracer.TraceInfo(message);
            DevelopmentLogData logData = new DevelopmentLogData
            {
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                ThreadName = Thread.CurrentThread.Name,
                DateTime = DateTime.Now,
                AdditionalInfo = message
            };
            DevelopmentLogger.LogInfo(ModuleId, eventIdInfo, logData);
        }

        /// <summary>
        /// Generates a random file name for staging area.
        /// Removes the extension and appends .dat as the common extension
        /// using this to avoid returning executables like .exe and .bat
        /// </summary>
        /// <returns>Random file name with .dat extension</returns>
        internal static string GetRandomFileName()
        {
            return Path.ChangeExtension(Path.GetRandomFileName(), ".dat");
        }

        /// <summary>
        /// Given the image dicom object, returns if it is a Multi-Frame
        /// </summary>
        /// <param name="imageDicomObject">the image dicom object</param>
        /// <returns>True if its MultiFrame, False otherwise</returns>
        internal static bool IsMultiFrameData(AIIDicomObject imageDicomObject)
        {
            int numberOfFrames;
            return IsMultiFrameData(imageDicomObject, out numberOfFrames);
        }

        /// <summary>
        /// Given the image dicom object, returns if it is a Multi-Frame
        /// </summary>
        /// <param name="imageDicomObject">the image dicom object</param>
        /// <param name="numberOfFrames">number of frames in the image</param>
        /// <returns>True if its MultiFrame, False otherwise</returns>
        internal static bool IsMultiFrameData(
            AIIDicomObject imageDicomObject,
            out int numberOfFrames
        )
        {
            int? dicomNumberOfFrames =
                imageDicomObject.GetInt32(DicomDictionary.DicomNumberOfFrames);
            numberOfFrames = dicomNumberOfFrames ?? 1;
            string sopClassUid =
                imageDicomObject.GetString(DicomDictionary.DicomSopClassUid);
            return
                (numberOfFrames > 1) ||
                UidGenerator.IsMultiframeSopClass(sopClassUid);
        }

        /// <summary>
        /// Gets the QueryLevel from the Storagekey
        /// </summary>
        /// <param name="storageKey"></param>
        /// <returns></returns>
        internal static QueryLevel GetQueryLevel(StorageKey storageKey)
        {
            if (string.IsNullOrEmpty(storageKey.Identifier.StudyInstanceUid))
            {
                return QueryLevel.Patient;
            }
            if (string.IsNullOrEmpty(storageKey.Identifier.SeriesInstanceUid))
            {
                return QueryLevel.Study;
            }
            if (string.IsNullOrEmpty(storageKey.Identifier.SopInstanceUid))
            {
                return QueryLevel.Series;
            }
            return QueryLevel.Image;
        }

        /// <summary>
        /// Classifies the storageKeys in different levels
        /// </summary>
        /// <param name="storageKeyCollection"></param>
        /// <param name="patientStorageKeys"></param>
        /// <param name="studyStorageKeys"></param>
        /// <param name="serieStorageKeys"></param>
        /// <param name="imageStorageKeys"></param>
        internal static void FilterStorageKeyCollection(
            StorageKeyCollection storageKeyCollection,
            out List<StorageKey> patientStorageKeys,
            out List<StorageKey> studyStorageKeys,
            out List<StorageKey> serieStorageKeys,
            out List<StorageKey> imageStorageKeys
        )
        {
            patientStorageKeys = null;
            studyStorageKeys = null;
            serieStorageKeys = null;
            imageStorageKeys = null;

            foreach (var storageKey in storageKeyCollection)
            {
                var identifier = storageKey.Identifier;
                if (identifier != null)
                {
                    if (!string.IsNullOrEmpty(identifier.SopInstanceUid))
                    {
                        if (imageStorageKeys == null)
                        {
                            imageStorageKeys = new List<StorageKey>();
                        }
                        if (!imageStorageKeys.Contains(storageKey))
                        {
                            imageStorageKeys.Add(storageKey);
                        }
                        continue;
                    }

                    if (!string.IsNullOrEmpty(identifier.SeriesInstanceUid))
                    {
                        if (serieStorageKeys == null)
                        {
                            serieStorageKeys = new List<StorageKey>();
                        }
                        if (!serieStorageKeys.Contains(storageKey))
                        {
                            serieStorageKeys.Add(storageKey);
                        }
                        continue;
                    }

                    if (!string.IsNullOrEmpty(identifier.StudyInstanceUid))
                    {
                        if (studyStorageKeys == null)
                        {
                            studyStorageKeys = new List<StorageKey>();
                        }
                        if (!studyStorageKeys.Contains(storageKey))
                        {
                            studyStorageKeys.Add(storageKey);
                        }
                        continue;
                    }
                    if (identifier.PatientKey != null)
                    {
                        if (patientStorageKeys == null)
                        {
                            patientStorageKeys = new List<StorageKey>();
                        }
                        if (!patientStorageKeys.Contains(storageKey))
                        {
                            patientStorageKeys.Add(storageKey);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Invalid storage key found.");
                    }
                }
            }
        }

        /// <summary>
        /// Checking the Filter Condition and returning the Appropriate
        /// result of QueryFilter
        /// </summary>
        /// <param name="dicomObjectForQuery">The dicom object for query.</param>
        /// <param name="removeConvertedSeriesFromQueryResults">if set to <c>true</c> 
        /// [ignore converted series in cases such as ECT, EMR, Mosaic ].</param>
        /// <param name="level">The query level.</param>
        /// <returns></returns>
        public static QueryFilter ConvertDicomObjectToQueryFilter(
            AIIDicomObject dicomObjectForQuery,
            bool removeConvertedSeriesFromQueryResults = false,
            QueryLevel level = QueryLevel.Study)
        {
            QueryFilter dbQueryFilter = QueryFilter.MatchAll();
            List<QueryFilter> queryFilterList = new List<QueryFilter>();

            ReadOnlyArray<DictionaryTag> queryTags = dicomObjectForQuery.GetTags();
            foreach (var queryTag in queryTags)
            {
                if (queryTag == DicomDictionary.DicomSpecificCharacterSet)
                {
                    // The Specific Character Set (0008,0005) Attribute may be present in the request
                    // but is never matched.Rather, it specifies how other Attributes are encoded
                    // in the Request and Response.
                    continue;
                }
                if (dicomObjectForQuery.HasValue(queryTag))
                {
                    object queryTagValueObject = dicomObjectForQuery.GetValue(queryTag);
                    Type type = queryTagValueObject.GetType();
                    if (type == typeof(string[]))
                    {
                        string[] queryTagValueArray = queryTagValueObject as string[];
                        tracer.TraceInfo("MatchAny of Query Tag :" + queryTag +
                            "is added to the QueryFilterList");
                        queryFilterList.Add(QueryFilter.MatchAny(queryTag, queryTagValueArray));
                    }
                    else if (
                        type == typeof(DicomObject[]) ||
                        type == typeof(AIIDicomObject[])
                    )
                    {
                        DicomObject[] seqObjects =
                            queryTagValueObject as DicomObject[];
                        foreach (var seqObject in seqObjects)
                        {
                            ReadOnlyArray<DictionaryTag> tags = seqObject.GetTags();
                            foreach (var tag in tags)
                            {
                                if (seqObject.HasValue(tag))
                                {
                                    //only handling the case needed for IHE compliance
                                    queryFilterList.Add(QueryFilter.MatchSequence(queryTag,
                                        QueryFilter.MatchExact(tag, seqObject.GetString(tag))));
                                }
                            }
                        }
                    }
                    else
                    {
                        // ToDo: check all queryable tags VR type ToString() implementation
                        // default assumption is tag value will be converted to 
                        // its string representation
                        string queryTagValue = queryTagValueObject.ToString();
                        if (queryTagValue == "*" && queryTag == DicomDictionary.DicomPatientName)
                        {
                            tracer.TraceInfo("DicomPatientName Tag is equal to '*' ");
                            continue;
                        }
                        if (
                            queryTag.ValueRepresentation == DicomVR.DA ||
                            queryTag.ValueRepresentation == DicomVR.DT
                        )
                        {
                            string[] range = queryTagValue.Split('-');
                            if (
                                range != null && range.Length == 2 &&
                                !string.IsNullOrEmpty(range[0]) &&
                                !string.IsNullOrEmpty(range[1])
                            )
                            {
                                // range matching
                                // convert date time to string
                                DateTime? first = ConvertStringToDateFormat(range[0], true);
                                DateTime? second = ConvertStringToDateFormat(range[1], true);
                                queryFilterList.Add(
                                    QueryFilter.MatchRange(
                                        queryTag, (DateTime)first, (DateTime)second
                                    )
                                );
                            }
                            else if (queryTagValue.StartsWith("-", StringComparison.Ordinal))
                            {
                                // Exact matching
                                DateTime? dateValue = ConvertStringToDateFormat(
                                    queryTagValue.Substring(1), true
                                );
                                queryFilterList.Add(
                                    QueryFilter.LessThan(queryTag, (DateTime)dateValue)
                                );
                            }
                            else if (queryTagValue.EndsWith("-", StringComparison.Ordinal))
                            {
                                // Exact matching
                                DateTime? dateValue = ConvertStringToDateFormat(
                                    queryTagValue.Substring(0, queryTagValue.Length - 1),
                                    true
                                    );
                                queryFilterList.Add(
                                    QueryFilter.GreaterThan(queryTag, (DateTime)dateValue)
                                    );

                            }
                            else
                            {
                                // Exact matching
                                DateTime? dateValue = ConvertStringToDateFormat(
                                    queryTagValue, false
                                    );
                                queryFilterList.Add(
                                    QueryFilter.MatchExact(queryTag, (DateTime)dateValue)
                                    );
                            }
                        }
                        else if (queryTag.ValueRepresentation == DicomVR.UI)
                        {
                            // handle study uid matching
                            if (queryTagValue.Contains("\\"))
                            {
                                queryFilterList.Add(
                                    QueryFilter.MatchAny(queryTag, queryTagValue.Split('\\'))
                                    );
                            }
                            else
                            {
                                queryFilterList.Add(QueryFilter.MatchExact(
                                    queryTag, queryTagValue));
                            }
                        }
                        else if (queryTagValue.Contains("*"))
                        {
                            tracer.TraceInfo(
                                "MatchWildCard of Query Tag :" + queryTag +
                                ", Query Tag Value : " + queryTagValue.Anonymize() +
                                " is been added to the QueryFilterList");
                            queryFilterList.Add(QueryFilter.MatchWildcard(queryTag, queryTagValue));
                        }
                        else
                        {
                            tracer.TraceInfo(
                                "MatchExact of Query Tag :" + queryTag +
                                ", Query Tag Value : " + queryTagValue.Anonymize() +
                                "is been added to the QueryFilterList");
                            queryFilterList.Add(QueryFilter.MatchExact(queryTag, queryTagValue));
                        }
                    }
                }
            }
            if (removeConvertedSeriesFromQueryResults)
            {
                queryFilterList.Add(
                    QueryFilter.MatchAny(
                    PhilipsDictionary.ElscintSourceSeriesUid,
                    (level == QueryLevel.Image)
                    ? new[] { string.Empty, null } : new[] { string.Empty }));
            }
            if (queryFilterList.Count > 0)
            {
                dbQueryFilter = QueryFilter.And(queryFilterList.ToArray());
            }

            return dbQueryFilter;
        }

        /// <summary>
        /// Returns the union of the fech results from <paramref name="first"/>
        /// and <paramref name="second"/> fetch results. If any of the input lists are empty,
        /// the other list is returned. If both the lists are non-empty, then the union
        /// of the 2 lists are returned. Note that either first or second could by themselves
        /// have duplicate entries, however any result that appears both in first and second
        /// will be returned only once.
        /// </summary>
        public static IReadOnlyCollection<FetchResult> UnionOFFetchResults(
            List<FetchResult> first, List<FetchResult> second
        )
        {
            List<FetchResult> union = new List<FetchResult>();
            HashSet<StorageKey> storageKeysInFirst = new HashSet<StorageKey>();

            if (first != null && second != null)
            {
                union.AddRange(first);
                storageKeysInFirst = new HashSet<StorageKey>(first.Select(x => x.StorageKey));

                if (second.Count > 0)
                {
                    foreach (var item in second)
                    {
                        if (!storageKeysInFirst.Contains(item.StorageKey))
                        {
                            union.Add(item);
                        }
                    }
                }

            }
            else if (first == null && second != null)
            {
                union.AddRange(second);
            }
            else if (first != null)
            {
                union.AddRange(first);
            }

            return union;
        }

        /// <summary>
        /// Checks if the sop class uid passed is raw pixel sop class.
        /// </summary>
        internal static bool IsRawPixelData(string sopClassUid)
        {
            return string.Equals(
                sopClassUid,
                "1.2.840.10008.5.1.4.1.1.66",
                StringComparison.Ordinal
            );
        }

        /// <summary>
        /// check and throw exception when device is readonly
        /// </summary>
        /// <param name="dicomDirDeviceId"></param>
        /// <param name="errorMessage"></param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static void CheckDeviceIsReadOnly(string dicomDirDeviceId,
            string errorMessage)
        {
            DeviceConfiguration deviceConfig = null;
            MediaRepositoryConfiguration mediaDeviceConfig = null;
            try
            {
                var deviceType = StorageDeviceProvider.GetDeviceType(dicomDirDeviceId);
                if (
                    deviceType == DeviceTypes.DicomUsbRepository ||
                    deviceType == DeviceTypes.DicomRemovableMediaRepository
                    )
                {
                    //check for device configuration of Media Device.
                    mediaDeviceConfig =
                        MediaDeviceManager.Instance.GetDeviceConfiguration(dicomDirDeviceId);
                }
                else
                {
                    //check for device configuration of StorageDevice
                    deviceConfig = ConfigurationManager.Instance.GetDeviceConfig(dicomDirDeviceId);
                }
            }
            catch (Exception)
            {
                // ignored when device is not present
            }
            if (
                mediaDeviceConfig != null && !mediaDeviceConfig.MediaInformation.Writable ||
                deviceConfig != null && deviceConfig.IsReadOnly
                )
            {
                LogError(errorMessage);
                throw new NotSupportedException(errorMessage);
            }
        }

        private static DateTime? ConvertStringToDateFormat(
            string val, bool isRangeDate
        )
        {
            DateTime date;
            if (isRangeDate)
            {
                if (
                    DateTime.TryParseExact(
                        val,
                        CultureInfo.InvariantCulture.DateTimeFormat.GetAllDateTimePatterns(),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out date)
                )
                {
                    return date;
                }
                date = DateTime.MinValue;
            }
            else
            {
                if (
                    DateTime.TryParseExact(
                        val,
                        CultureInfo.CurrentCulture.DateTimeFormat.GetAllDateTimePatterns(),
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.None,
                        out date)
                )
                {
                    return date;
                }
                date = DateTime.MinValue;
            }
            return date;
        }

        /// <summary>
        /// Gets the approximate size of the <paramref name="dicomObject"/>.<br/>
        /// Use this method cautiously as this might trigger GC and can cause
        /// more allocations.<br/>
        /// !!!...Do not rely on the size returned by this method for any practical purposes...!!!
        /// </summary>
        /// <param name="dicomObject">The dicom object.</param>
        /// <returns></returns>
        public static long GetApproximateSize(this DicomObject dicomObject)
        {
            /* 
            * 
            * almost perfect way to get managed size...
            * but this requires GC to be done
            * via GC.GetTotalMemory(true);            
            * and this also depends on allocations from other threads...
            * so not ideal to use in a system wide scenarios...
            * 
            DicomObject dcm1 = new DicomObject();
            dcm1.Combine(dicomObject);
            long size1 = GC.GetTotalMemory(true);
            DicomObject dcm2 = new DicomObject();
            dcm2.Combine(dicomObject);
            long size2 = GC.GetTotalMemory(true);
            dcm1.Dispose();
            dcm2.Dispose();
            return size2-size1;*/
            long length;
            using (var ms = new RecyclableBufferMemoryStream(1024 * 1024))
            {
                using (var bw = new BinaryWriter(ms))
                {
                    BinarySerializer.Store(bw, dicomObject);
                    length = ms.Length;
                }
            }
            // it was consistently observed that the length got using binary serialization
            // is at least half as less as that got by above commented code.
            // * 2 is on 64 bit process...!!

            // If above is true there should be a test which validates this against .NET framework updates...
            return length * 2;
        }

        /// <summary>
        /// Method to trace flow of events.
        /// </summary>
        /// <param name="message">message to log</param>
        public static void TraceFlow(string message)
        {
            if (IsFlowTracingEnabled)
            {
                flowTracer.TraceInfo("SDFlow: " + message);
            }
        }

        /// <summary>
        /// Method to trace statistics.
        /// </summary>
        /// <param name="message">message to log</param>
        public static void TraceStatistics(string message)
        {
            if (IsStatisticsTracingEnabled)
            {
                statisticsTracer.TraceInfo("SDStats: " + message);
            }
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exp">The exp.</param>
        public static void LogError(string message, Exception exp)
        {
            DevelopmentLogData data = new DevelopmentLogData()
            {
                AdditionalInfo = message,
                Exception = exp
            };
            DevelopmentLogger.LogError(ModuleId, eventIdError, data);
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void LogWarning(string message)
        {
            DevelopmentLogData developmentLogData = new DevelopmentLogData()
            {
                AdditionalInfo = message
            };

            DevelopmentLogger.LogWarning(ModuleId, eventIdWarning, developmentLogData);
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void LogError(string message)
        {
            DevelopmentLogData developmentLogData = new DevelopmentLogData()
            {
                AdditionalInfo = message
            };

            DevelopmentLogger.LogError(ModuleId, eventIdError, developmentLogData);
        }

        /// <summary>
        /// Logs the service message.
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="msg"></param>
        public static void LogServiceMsg(int eventId, string[] msg)
        {
            ServiceLogger.LogSoftwareApplicationError(
                LoggingConstants.DatabaseModuleId, eventId, msg
            );
        }

        /// <summary>
        /// The BulkDataReference path denoted by Elscint1BulkFileReference
        /// attribute in the dicom object is converted from relative path to full path.
        /// </summary>
        public static string ResolveBulkDataReferenceFilePath(
            DicomObject dicomObject,
            string deviceId,
            SubOperationTimer timer = null,
            string resolvedPath = null
        )
        {
            if (timer != null)
            {
                timer.NumberOfOperations++;
                timer.Timer.Start();
            }

            string bulkPath = string.Empty;
            BulkDataReference bulkRef = dicomObject.GetBulkDataReference(
                GetPixelDataTag(dicomObject.GetTagVR(DicomDictionary.DicomPixelData)));
            if (bulkRef != null)
            {
                // resolve the bulk file reference and BDR for dicom files
                if (
                    !string.IsNullOrEmpty(bulkRef.FileName) &&
                    bulkRef.FileName.StartsWith("\\", StringComparison.OrdinalIgnoreCase)
                )
                {
                    bulkPath = GetFullPath(bulkRef.FileName, deviceId, resolvedPath);
                    BulkDataReference bdr =
                        new BulkDataReference(bulkPath, bulkRef.Offset, bulkRef.Length);
                    dicomObject.SetBulkDataReference(
                        GetPixelDataTag(dicomObject.GetTagVR(DicomDictionary.DicomPixelData)),
                        bdr);
                    dicomObject.UpdateBDR(bulkPath, bulkRef.FileName);
                    if (dicomObject.HasTag(PhilipsDictionary.Elscint1BulkFileReference))
                    {
                        dicomObject.SetString(
                            PhilipsDictionary.Elscint1BulkFileReference, bulkPath);
                    }
                }
            }
            else
            {
                // resolve the bulk file reference for non dicom files
                if (dicomObject.HasTag(PhilipsDictionary.Elscint1BulkFileReference))
                {
                    bulkPath = dicomObject.GetString(
                        PhilipsDictionary.Elscint1BulkFileReference);
                    if (bulkPath.StartsWith("\\", StringComparison.OrdinalIgnoreCase))
                    {
                        var fullPath = GetFullPath(bulkPath, deviceId, resolvedPath);
                        dicomObject.SetString(
                            PhilipsDictionary.Elscint1BulkFileReference, fullPath);
                        dicomObject.UpdateBDR(fullPath, bulkPath);
                        bulkPath = fullPath;
                    }
                }
            }

            if (timer != null)
            {
                timer.Timer.Stop();
            }

            return bulkPath;
        }

        /// <summary>
        /// The representative image path denoted by ElscintRepresentativeImagePath
        /// attribute in the dicom object is converted from relative path to full path.
        /// </summary>
        public static void ResolveRepresentativeImagePath(
            DicomObject dicomObj,
            string deviceId,
            string resolvedPath = null
        )
        {

            //First resolve path for sequence attributes(for now only "surview" sequence is known
            //which contains the representative image path) - ISP00167946 - ISP8/Surview Path
            if (
                dicomObj != null &&
                dicomObj.HasTag(PhilipsDictionary.ElscintSurviewImageSequence)
            )
            {
                var surviewImages = dicomObj.GetDicomObject(
                    PhilipsDictionary.ElscintSurviewImageSequence);
                foreach (DicomObject surviewImage in surviewImages)
                {
                    ResolveRepresentativeImagePath(
                        surviewImage, deviceId, resolvedPath);
                }
            }

            if (
                dicomObj != null &&
                dicomObj.HasTag(PhilipsDictionary.ElscintRepresentativeImagePath)
            )
            {
                string bulkPath = dicomObj.GetString(
                    PhilipsDictionary.ElscintRepresentativeImagePath);
                if (bulkPath.StartsWith("\\", StringComparison.OrdinalIgnoreCase))
                {
                    dicomObj.SetString(
                        PhilipsDictionary.ElscintRepresentativeImagePath,
                        GetFullPath(bulkPath, deviceId, resolvedPath)
                    );
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static string GetRelativePath(string fullPath, string deviceId)
        {
            string storePath = ConfigurationManager.Instance.GetBulkFolderDirectory(deviceId);
            if (!string.IsNullOrEmpty(fullPath) && fullPath.Contains(storePath))
            {
                return fullPath.Remove(0, storePath.Length);
            }
            return fullPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="deviceId"></param>
        /// <param name="resolvedPath"></param>
        /// <returns></returns>
        public static string GetFullPath(
            string relativePath, string deviceId, string resolvedPath = null
        )
        {
            if (!string.IsNullOrWhiteSpace(resolvedPath))
            {
                return resolvedPath + relativePath;
            }

            string storePath = ConfigurationManager.Instance.GetBulkFolderDirectory(deviceId);
            if (!string.IsNullOrEmpty(relativePath) && !relativePath.Contains(storePath))
            {
                return relativePath.Insert(0, storePath);
            }
            return relativePath;
        }

        internal static string GetLocalHostNameOrIpAddress()
        {
            if (!string.IsNullOrWhiteSpace(localHostNameOrIpAddress))
            {
                return localHostNameOrIpAddress;
            }
            var serverHostNameOrIpAddress =
                ConfigurationManager.Instance.RemoteSettings.HostNameOrIPAddress;
            var hostName = Dns.GetHostName();
            bool dnsResolutionRequired = false;
            if (
                !string.Equals(
                    serverHostNameOrIpAddress, "localhost", StringComparison.OrdinalIgnoreCase)
            )
            {
                if (
                    !string.Equals(
                        serverHostNameOrIpAddress, hostName, StringComparison.OrdinalIgnoreCase)
                )
                {
                    IPAddress serverIpAddress;
                    if (IPAddress.TryParse(serverHostNameOrIpAddress, out serverIpAddress))
                    {
                        // is localhost {127.0.0.1}
                        if (IPAddress.IsLoopback(serverIpAddress))
                        {
                            dnsResolutionRequired = true;
                        }
                    }
                }
            }
            else
            {
                dnsResolutionRequired = true;
            }

            if (dnsResolutionRequired)
            {
                serverHostNameOrIpAddress = GetIpAddress(hostName) ?? hostName;
            }
            localHostNameOrIpAddress = serverHostNameOrIpAddress;
            return localHostNameOrIpAddress;
        }

        private static string GetIpAddress(string hostName)
        {
            IPAddress[] ipAddresses = Dns.GetHostAddresses(hostName);
            string ipAddress = null;
            foreach (var address in ipAddresses)
            {
                if (
                    address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address))
                {
                    ipAddress = address.ToString();
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                ipAddress = ipAddresses[0].ToString();
            }
            return ipAddress;
        }

        internal static bool DoesMetaFileExist(string seriesDir)
        {
            if (!string.IsNullOrWhiteSpace(seriesDir))
            {
                return File.Exists(GetMetaFilePath(seriesDir));
            }

            return false;
        }

        /// <summary>
        /// Gets the meta file path for the series.
        /// </summary>
        internal static string GetMetaFilePath(string seriesDir)
        {
            return Path.Combine(seriesDir, FileNameConstants.MetaFile);
        }

        /// <summary>
        /// Gets the index file path for the series.
        /// </summary>
        internal static string GetIndexFilePath(string seriesDir)
        {
            return Path.Combine(seriesDir, FileNameConstants.IndexFile);
        }

        /// <summary>
        /// Returns Patient UID for indexing.
        /// </summary>
        /// <param name="patientKey"></param>
        /// <returns></returns>
        public static string GetPatientUidForPatient(PatientKey patientKey)
        {
            return PatientKeyUtility.GetPatientUidFromPatientKey(patientKey);
        }
        //TICS -7@304: Only use optional arguments to replace overloads
        /// <summary>
        /// Grant Database access to windows user groups.
        /// </summary>
        /// <param name="dataBase"></param>
        /// <param name="connectionstring"></param>
        public static void GrantDbAccessToWindowsUserGroup(
            string dataBase, string connectionstring)
        {
            // Temporarily DB is handling it. It has to be handled by login component.
            // Access should be given for database 
            // if database name is empty in List<GrantAccessToUserGroup>.
            var userGroupList = ((List<GrantAccessToUserGroup>)
                ConfigurationManager.Instance.GrantAccessToUserGroupList).FindAll(
                    x => ((string.IsNullOrWhiteSpace(x.SqlDatabaseName)) ||
                        x.SqlDatabaseName.Equals(dataBase)));
            string userGroups = string.Empty;
            string commandText = string.Empty;
            if (userGroupList.Count > 0)
            {
                Console.WriteLine("UserGroupList is not empty");
                foreach (var grantAccessToUserGroup in userGroupList)
                {
                    commandText +=
                        String.Format(
                            CultureInfo.CurrentCulture,
                            "use master;" +
                            "If not Exists (SELECT name  FROM master.sys.server_principals " +
                            "WHERE sid = SUSER_SID(N'{0}')) " +
                            "Begin " +
                            "create Login [{0}] from windows; " +
                            "Create user [{0}] from Login [{0}]; " +
                            "GRANT VIEW ANY DEFINITION TO [{0}]; " +
                            "End; " +
                            "use {1};" +
                            "IF NOT EXISTS(SELECT name FROM sys.database_principals " +
                            "WHERE sid = SUSER_SID(N'{0}')) " +
                            "BEGIN " +
                            "Create user [{0}] from Login [{0}]; " +
                            "grant connect to [{0}]; " +
                            "exec sp_addrolemember 'db_datareader',[{0}]; " +
                            "exec sp_addrolemember 'db_datawriter',[{0}]; " +
                            "END; ",
                            grantAccessToUserGroup.UserGroup,
                            dataBase
                            );
                    userGroups += grantAccessToUserGroup.UserGroup + "; ";
                }
                userGroups = userGroups.Remove(userGroups.Length - 2);
                using (SqlConnection con = new SqlConnection(connectionstring))
                {
                    try
                    {
                        con.Open();
                        using (SqlCommand cmd = new SqlCommand(commandText, con))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        Console.WriteLine("Database access has been granted to" +
                            "usergroups is succesfully");
                        // Log audit trail message
                        SATAuditTrailToolkit.AuditSecurityAlert(
                            (SATEventOutcome)EventOutcome.Success,
                            AuthorizationManager.GetCurrentUser().Name,
                            Utilities.ProcessUtilities.ProcessName,
                            null,
                            false,
                            null,
                            SATSecurityAlertType.SoftwareConfiguration,
                            "Database access has been granted to usergroups - " + userGroups
                        );
                    }
                    catch (SqlException ex)
                    {
                        LogError(
                            "Error while granting permissions to non admin user: ",
                            ex);
                    }
                }
            }
        }

        /// <summary>
        /// Return filestream object based on config.
        /// </summary>
        /// <param name="filePath">file path</param>
        /// <param name="fileMode">file mode</param>
        /// <param name="fileAccess">file access type</param>
        /// <param name="fileShare">file share</param>
        /// <param name="logException">log exception or not</param>
        /// <returns>FileStream object</returns>
        public static FileStream GetFileStreamObject(
            string filePath,
            FileMode fileMode,
            FileAccess fileAccess,
            FileShare fileShare,
            bool logException = true
        )
        {
            FileStream fs;
            try
            {
                if (
                    ConfigurationManager.Instance.GlobalConfiguration.EnableFlushToDisk &&
                    fileAccess != FileAccess.Read
                )
                {
                    fs = new FileStream(
                        filePath,
                        fileMode,
                        fileAccess,
                        fileShare,
                        4 * 1024,
                        FileOptions.WriteThrough);
                }
                else
                {
                    fs = new FileStream(
                        filePath,
                        fileMode,
                        fileAccess,
                        fileShare);
                }
            }
            catch (FileNotFoundException e)
            {
                tracer.TraceInfo("File doesn't exists: " + e.FileName.Anonymize());
                throw;
            }
            catch (DirectoryNotFoundException e)
            {
                tracer.TraceInfo("Directory doesn't exists: " + e.Message.Anonymize());
                throw;
            }
            catch (IOException e)
            {
                TraceFileUsage(filePath);
                if (logException)
                {
                    tracer.TraceInfo("Exception during reading file " + e.Message);
                }
                throw;
            }
            catch (UnauthorizedAccessException e)
            {
                tracer.TraceInfo("Doesn't have required permission " + e.Message);
                throw;
            }

            return fs;
        }

        private static void TraceFileUsage(string filepath)
        {
            var processInUse = ProcessLockFileHunter.GetListOfProcess(filepath);
            if (processInUse != null)
            {
                foreach (var process in processInUse)
                {
                    tracer.TraceInfo("File:: " + filepath + " is being used by::" + process);
                }
            }
        }

        internal static void MoveTempFileToNormalFile(string tempFile, string normalFile)
        {
            int currCount = 1;
            bool success = false;
            int retryCount = 50;
            Exception exception = null;
            do
            {
                try
                {
                    if (File.Exists(normalFile))
                    {
                        File.Delete(normalFile);
                    }
                    success = true;
                }
                catch (IOException ex)
                {
                    exception = ex;
                    // 100ms x 50 times = 5000ms
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                }
            } while (!success && currCount++ <= retryCount);

            if (!success)
            {
                tracer.TraceInfo(
                    "Unable to delete " + normalFile + " even after " +
                    retryCount + " retries.", exception);
            }
            MoveFile(tempFile, normalFile);
        }

        private static void MoveFile(string sourceFile, string targetFile)
        {
            int currentCount = 1;
            bool isFileMoved = false;
            int retryCount = 50;
            IOException exception = null;
            do
            {
                try
                {
                    File.Move(sourceFile, targetFile);
                    isFileMoved = true;
#pragma warning disable PFB4327 // Log caught exceptions.
                }
                catch (IOException ex)
                {
                    exception = ex;
                    // 100ms x 50 times = 5000ms
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                }
#pragma warning restore PFB4327 // Log caught exceptions.
            } while (!isFileMoved && currentCount++ <= retryCount);
            if (!isFileMoved)
            {
                tracer.TraceInfo("Unable to move " + sourceFile + " even after " +
                    retryCount + " retries.", exception);
                throw exception;
            }
        }

        internal static void DeleteFile(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
            catch (IOException)
            {
                //ignore
            }
        }

        /// <summary>
        /// Deletes the given file in Async.
        /// </summary>
        internal static void DeleteFileAsync(string fileName)
        {
            Task.Factory.StartNew(() => {
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    try
                    {
#pragma warning disable PFB4327 // Log caught exceptions.
                        if (File.Exists(fileName))
                        {
                            File.Delete(fileName);
                        }
                    }
                    catch (IOException ex)
                    {
                        LogError("Exception while deleting the file", ex);
                    }
#pragma warning restore PFB4327 // Log caught exceptions.
                }
            });
        }

        /// <summary>
        /// Flsuh file stream based on config
        /// </summary>
        /// <param name="fs">filestream object</param>
        /// <returns>status as true for success false for failure.</returns>
        public static bool FlushStream(FileStream fs)
        {
            bool status = true;
            fs.Flush();
            if (ConfigurationManager.Instance.GlobalConfiguration.EnableFlushFileBuffers)
            {
                if (!NativeMethods.FlushFileBuffers(fs.SafeFileHandle))
                {
                    status = false;
                    int lastError = Marshal.GetLastWin32Error();
                    string msg =
                        "FlushFileBuffers failed with error code : " +
                        lastError.ToString(CultureInfo.InvariantCulture) +
                        ", Exception : " + new Win32Exception(lastError).Message;
                    LogMessage(msg);
                }
            }
            return status;
        }

        internal static string GetFilePath(
            string seriesDir,
            SeriesFileType fileType,
            string sopInstanceUid = ""
        )
        {
            switch (fileType)
            {
                case SeriesFileType.Fast:
                    return Path.Combine(seriesDir, FileNameConstants.ImageFastFile);
                case SeriesFileType.Full:
                    return Path.Combine(seriesDir, FileNameConstants.ImageFullFile);
                case SeriesFileType.Index:
                    return Path.Combine(seriesDir, FileNameConstants.IndexFile);
                case SeriesFileType.Frame:
                    return Path.Combine(
                        seriesDir, sopInstanceUid + FileNameConstants.FrameFileSuffix
                    );
                case SeriesFileType.Dirty:
                    return Path.Combine(seriesDir, FileNameConstants.DirtyFileName);
                case SeriesFileType.AcquisitionInProgress:
                    return Path.Combine(seriesDir, FileNameConstants.MarkerForUnderConstruction);
                default:
                    return "";
            }
        }

        private static readonly Dictionary<SeriesFileType, List<SeriesFileType>> complementsOf =
            new Dictionary<SeriesFileType, List<SeriesFileType>>() {
                //for the Fast file, the complements are Full and Index files
                {
                    SeriesFileType.Fast, new List<SeriesFileType>() {
                        SeriesFileType.Full
                    }
                }, {
                    SeriesFileType.Full, new List<SeriesFileType>() {
                        SeriesFileType.Fast
                    }
                }, {
                    SeriesFileType.Index, new List<SeriesFileType>() {
                        SeriesFileType.Fast,
                        SeriesFileType.Full
                    }
                }, {
                    SeriesFileType.Frame, new List<SeriesFileType>() {
                        SeriesFileType.Fast,
                        SeriesFileType.Full,
                        SeriesFileType.Index
                    }
                }, {
                    SeriesFileType.Dirty, new List<SeriesFileType>()
                }
            };

        private static bool FileExists(
            string seriesDir,
            SeriesFileType fileType,
            string sopInstaceUid = ""
        )
        {
            return File.Exists(GetFilePath(seriesDir, fileType, sopInstaceUid));
        }

        private static bool AnyComplementFilesExist(
            string seriesDir,
            SeriesFileType fileType,
            string sopInstanceUid = ""
        )
        {
            return complementsOf[fileType].Any(f => FileExists(seriesDir, f, sopInstanceUid));
        }

        /// <summary>
        /// Checks if the given file types are present under the series
        /// and throws an exception if the requested files are missing but some other files 
        /// are present.
        /// 
        /// No exception will be thrown if all the files are missing under the series.
        /// This condition implies that the series is still integral (except that no file
        /// writes have been done under the series)
        /// </summary>
        /// <param name="seriesDir">
        /// The series directory.
        /// </param>
        /// <param name="fileTypes">
        /// We check for existence of these file type under the series.
        /// </param>
        /// <param name="seriesStorageKey">
        /// The series storage key.
        /// </param>
        /// <param name="allFilesMissing">
        /// Indicates whether all the files are missing under the series.
        /// </param>
        /// <remarks>
        /// If all the files under a series are not present, then that series
        /// is considered a valid series.
        /// 
        /// However if some files are present and other files are not present 
        /// under the series, then that series is considered an invalid series.
        /// </remarks>
        internal static void CheckAndHandleMissingFilesUnderSeries(
            string seriesDir,
            List<SeriesFileType> fileTypes,
            StorageKey seriesStorageKey,
            out bool allFilesMissing
        )
        {
            CheckAndHandleMissingFilesUnderSeries(
                seriesDir,
                fileTypes,
                seriesStorageKey,
                "", //null, since this function is only for dealing with non-frame header files
                out allFilesMissing
            );
        }

        /// <summary>
        /// Checks if the given file types are present under the series
        /// and throws an exception if the requested files are missing but some other files 
        /// are present.
        /// 
        /// No exception will be thrown if all the files are missing under the series.
        /// This condition implies that the series is still integral (except that no file
        /// writes have been done under the series)
        /// </summary>
        /// <param name="seriesDir">
        /// The series directory.
        /// </param>
        /// <param name="fileTypes">
        /// We check for existence of these file type under the series.
        /// </param>
        /// <param name="seriesStorageKey">
        /// The series storage key.
        /// </param>
        /// <param name="sopInstanceUid">
        /// The sop instance uid of the image (applicable only when the file
        /// that is checked for is the frame header file)
        /// </param>
        /// <param name="allFilesMissing">
        /// Indicates whether all the files are missing under the series.
        /// </param>
        /// <remarks>
        /// If all the files under a series are not present, then that series
        /// is considered a valid series.
        /// 
        /// However if some files are present and other files are not present 
        /// under the series, then that series is considered an invalid series.
        /// </remarks>
        internal static void CheckAndHandleMissingFilesUnderSeries(
            string seriesDir,
            List<SeriesFileType> fileTypes,
            StorageKey seriesStorageKey,
            string sopInstanceUid,
            out bool allFilesMissing
        )
        {
            allFilesMissing = false;
            foreach (var fileType in fileTypes)
            {
                bool fileExists = FileExists(seriesDir, fileType, sopInstanceUid);
                if (!fileExists)
                {
                    //todo : optimize, there will be a overlap in the complement file list
                    //todo : don't need to check twice for the same file.
                    if (AnyComplementFilesExist(seriesDir, fileType, sopInstanceUid))
                    {
                        if (fileType == SeriesFileType.Fast)
                        {
                            //Added logs to check if any race condition in updating fast header
                            if (
                                File.Exists(
                                Path.Combine(seriesDir, FileNameConstants.TempImageFastFile)))
                            {
                                LogMessage("Temporary Fast header file exist");
                            }
                            LogMessage(
                                "Series encountered, Missing file: FastHeader " +
                                seriesDir.Anonymize());
                        }
                        else if (fileType == SeriesFileType.Full)
                        {
                            LogMessage(
                                "Series encountered, Missing file: FullHeader " +
                                seriesDir.Anonymize());
                        }
                        else if (fileType == SeriesFileType.Index)
                        {
                            LogMessage(
                                "Series encountered, Missing file: Index " +
                                seriesDir.Anonymize());
                        }
                        LogAndThrowDataCorruptException(
                            "The series is corrupt since one or more files are " +
                            "missing from the file store.",
                            seriesStorageKey,
                            GetFilePath(seriesDir, fileType, sopInstanceUid),
                            null
                        );
                    }
                    else
                    {
                        allFilesMissing = true;
                    }
                }
            }

            //(if all the files are missing) and (if dirty file is also not present)
            //and (if acquisition under progress file is also not present)
            //then the series was actually committed earlier but it got corrupt
            //so we throw exception
            if (allFilesMissing)
            {
                bool dirtyFileExists = FileExists(seriesDir, SeriesFileType.Dirty);
                bool acquisitionUnderProgressFileExists =
                    FileExists(seriesDir, SeriesFileType.AcquisitionInProgress);
                var seriesExistsInDb = !string.IsNullOrWhiteSpace(SDStoreHelper.GetPathFromDatabase(
                    seriesStorageKey, Level.Series));
                if (
                    !dirtyFileExists &&
                    !acquisitionUnderProgressFileExists &&
                    seriesExistsInDb
                )
                {
                    LogMessage
                        ("Missing files in series : " + seriesDir.Anonymize() + "");
                    LogAndThrowDataCorruptException(
                        "The series is corrupt since one or more files are " +
                        "missing from the file store.",
                        seriesStorageKey,
                        GetFilePath(seriesDir, fileTypes[0], sopInstanceUid),
                        null
                    );
                }
                //else :: if dirty file exists, then no problem
                //the series is still under construction and we behave nicely
                //by not throwing any error.
            }
        }

        internal static void LogAndThrowDataCorruptException(
            string errorMessage,
            string deviceId,
            PatientKey patientKey,
            string studyUid,
            string seriesUid,
            string corruptFilePath,
            Exception innerException
        )
        {
            LogAndThrowDataCorruptException(
                errorMessage,
                FileStoreUtilities.CreateSeriesStorageKeyFrom(
                    deviceId,
                    patientKey,
                    studyUid,
                    seriesUid
                ),
                corruptFilePath,
                innerException
            );
        }

        internal static void LogAndThrowDataCorruptException(
            string errorMessage,
            StorageKey storageKey,
            string corruptFilePath,
            Exception innerException
        )
        {
            LogError(errorMessage, innerException);
            LogServiceMsg(
                LoggingConstants.DeviceInconsistencyEventId,
                new[] { errorMessage }
            );

            throw new DataCorruptException(
                errorMessage,
                storageKey,
                corruptFilePath,
                innerException
            );
        }

        /// <summary>
        /// Creates a marker file under the series. This is an indication
        /// that the series is not committed.
        /// </summary>
        /// <param name="seriesPath">
        /// The series that we want to make 'dirty' or "underConstruction".
        /// </param>
        /// <param name="fileName">
        /// The filename that we want to create under the series.
        /// </param>
        internal static void CreateMarkerFileUnderSeries(string seriesPath, string fileName)
        {
            tracer.TraceInfo(
                "Creating marker file for series :" +
                seriesPath.Anonymize() + "."
            );
            var markerFileName = Path.Combine(seriesPath, fileName);
            if (!File.Exists(markerFileName))
            {
                try
                {
                    new FileStream(
                        markerFileName,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.Delete
                        ).Close();
                }
                catch (IOException ex)
                {
                    string errormessage =
                        "Error while creating marker file - " + markerFileName.Anonymize();
                    LogError(errormessage, ex);
                }
            }
            else
            {
                FileInfo dirFileInfo = new FileInfo(markerFileName);
                dirFileInfo.LastWriteTime = DateTime.Now;
                tracer.TraceInfo(
                    "CreateMarkerFileUnderSeries. Marker file :" +
                    markerFileName.Anonymize() + " already exists."
                );
            }
        }

        /// <summary>
        /// Deletes the marker file under the series. This is an indication that
        /// the series is now committed.
        /// </summary>
        /// <param name="seriesPath">
        /// The series that we want to mark as committed.
        /// </param>
        /// <param name="fileName">
        /// The filename that we want to create under the series.
        /// </param>
        internal static void DeleteMarkerFileUnderSeries(string seriesPath, string fileName)
        {

            var markerFileName = Path.Combine(seriesPath, fileName);
            if (File.Exists(markerFileName))
            {
                try
                {
                    File.Delete(markerFileName);
                    tracer.TraceInfo(
                        "Deleted MarkerFile UnderSeries. Marker file :" +
                        markerFileName.Anonymize() + "."
                    );
                }
                catch (IOException ex)
                {
                    string errormessage =
                        "Error deleting marker file - " + markerFileName.Anonymize();
                    LogError(errormessage, ex);
                }
            }
            else
            {
                tracer.TraceInfo(
                    "DeleteMarkerFileUnderSeries. Marker file :" +
                    markerFileName.Anonymize() + " does not exist."
                );
            }
        }

        /// <summary>
        /// Gets the Series Header.
        /// </summary>
        /// <param name="seriesPath">The series path.</param>
        /// <returns>Series header object.</returns>
        internal static DicomObject GetSeriesHeader(string seriesPath)
        {
            var seriesFilePath = Path.Combine(seriesPath, FileNameConstants.SeriesFullFile);
            return FileStoreHelper.ReadDicomObjectFromFile(seriesFilePath);
        }

        /// <summary>
        /// Gets the PatientStudy Header.
        /// </summary>
        /// <param name="studyPath">The study path.</param>
        /// <returns>PatientStudy object.</returns>
        internal static DicomObject GetPatientStudyHeader(string studyPath)
        {
            var patientStudyPath =
                Path.Combine(studyPath, FileNameConstants.PatientStudyFullFile);
            return FileStoreHelper.ReadDicomObjectFromFile(patientStudyPath);
        }

        /// <summary>
        /// Checks whether the specified series folder contains any image file.
        /// </summary>
        /// <param name="seriseDirectoryInfo">Study directory info which will be checked.</param>
        /// <returns>True if the study contains image otherwise false.</returns>
        internal static bool IsContainImageFile(DirectoryInfo seriseDirectoryInfo)
        {
            bool isImageFilePresent = false;
            var imageDirectories = seriseDirectoryInfo.GetDirectories();
            if (imageDirectories.Any())
            {
                foreach (var imageDirectory in imageDirectories)
                {
                    if (!IsDirectoryEmpty(imageDirectory.FullName))
                    {
                        isImageFilePresent = true;
                        break;
                    }
                }
            }
            return isImageFilePresent;
        }

        internal static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        /// <summary>
        /// Checks whether the specified study folder stored via acquistion flow or not.
        /// It checks based on existance of patient study header file.
        /// </summary>
        /// <param name="studyDirectoryInfo">Study directory info which will be checked.</param>
        /// <returns>True if stored via SII otherwise false.</returns>
        internal static bool IsStudyStoredViaAcquisition(DirectoryInfo studyDirectoryInfo)
        {
            var patientStudyHeaderPath = Path.Combine(
                studyDirectoryInfo.FullName, FileNameConstants.PatientStudyFullFile);

            return File.Exists(patientStudyHeaderPath);
        }

        /// <summary>
        /// Combines the PatientStudy dicom object and Series dicom object.
        /// </summary>
        /// <param name="studyPath">Patient Study header file path.</param>
        /// <param name="seriesPath">The series path.</param>
        /// <returns>Combined object of PatientStudy and series dicom.</returns>
        internal static DicomObject GetCombinedStudySeriesHeader(
            string studyPath, string seriesPath
        )
        {
            var patientStudySeriesDicomObject = GetPatientStudyHeader(studyPath);
            if (patientStudySeriesDicomObject == null)
            {
                throw new FileLoadException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Header file {0} is corrupted.",
                        FileNameConstants.PatientStudyFullFile));
            }

            var seriesDicomObject = GetSeriesHeader(seriesPath);

            if (seriesDicomObject != null)
            {
                patientStudySeriesDicomObject.Combine(seriesDicomObject);
            }

            return patientStudySeriesDicomObject;
        }

        /// <summary>
        /// Checks whether the PatientStudy header file present or not.
        /// </summary>
        /// <param name="studyPath"></param>
        /// <returns>True if Patient study header file exists otherwise false.</returns>
        internal static bool IsPatientStudyHeaderFileExist(string studyPath)
        {

            var patientStudyPath =
                Path.Combine(studyPath, FileNameConstants.PatientStudyFullFile);
            return File.Exists(patientStudyPath);
        }

        /// <summary>
        /// Checks whether the series header file present or not.
        /// </summary>
        /// <param name="seriesPath">The series path.</param>
        /// <returns>True if series header file exists otherwise false.</returns>
        internal static bool IsSeriesHeaderFileExist(string seriesPath)
        {
            var seriesHeaderPath =
                Path.Combine(seriesPath, FileNameConstants.SeriesFullFile);
            return File.Exists(seriesHeaderPath);
        }

        /// <summary>
        /// Combines the PatientStudy dicom object and Series dicom object.
        /// </summary>
        /// <param name="studyPath">Patient Study header file path.</param>
        /// <param name="seriesPath">The series path.</param>
        /// <returns>Combined object of PatientStudy and series dicom.</returns>
        internal static DicomObject GetCombinedPatientStudySeriesHeader(
            string studyPath, string seriesPath)
        {
            DicomObject patientStudySeriesDicomObject = null;

            if (IsPatientStudyHeaderFileExist(studyPath))
            {
                patientStudySeriesDicomObject = GetPatientStudyHeader(studyPath);
            }

            if (!IsSeriesHeaderFileExist(seriesPath))
            {
                return patientStudySeriesDicomObject;
            }
            var seriesDicomObject = GetSeriesHeader(seriesPath);
            if (seriesDicomObject == null)
            {
                return patientStudySeriesDicomObject;
            }

            if (patientStudySeriesDicomObject != null)
            {
                // Handle Character Set.
                // Character set cannot be blindly replaced. We need to find the union
                // of the character sets involved and use that as the resultant character set.
                var resultantCharacterSet =
                    IdentifyResultantCharacterSet(
                        new List<DicomObject>() {
                            patientStudySeriesDicomObject, seriesDicomObject
                        });
                //combine both series and patient
                seriesDicomObject.Combine(patientStudySeriesDicomObject);
                SetCharSetValue(seriesDicomObject, resultantCharacterSet);
            }
            return seriesDicomObject;
        }

        /// <summary>
        /// Does a Union of the Character sets in the incoming object and returns a
        /// valid Character Set that can handle both the individual character sets.
        /// </summary>
        /// <param name="dicomObjects"></param>
        /// <returns></returns>
        internal static string IdentifyResultantCharacterSet(List<DicomObject> dicomObjects)
        {
            var charSets = new List<string>();
            foreach (var dicomObject in dicomObjects)
            {
                AddCharacterSetToList(
                    charSets, dicomObject.GetString(DicomDictionary.DicomSpecificCharacterSet));
            }
            var returnVal =
                CommonUtilities.Dicom.SpecificCharacterSetHelper.GetCharacterSetMapping(charSets);
            return returnVal;
        }

        /// <summary>
        /// Adds the character sets to incoming list.
        /// </summary>
        /// <param name="characterSetValues"></param>
        /// <param name="characterSet"></param>
        internal static void AddCharacterSetToList(
            List<string> characterSetValues, string characterSet)
        {
            if (string.IsNullOrWhiteSpace(characterSet)) return;
            foreach (var value in characterSet.Split('\\'))
            {
                if (
                    !string.IsNullOrWhiteSpace(value) &&
                    !characterSetValues.Contains(value))
                {
                    characterSetValues.Add(value);
                }
            }
        }

        /// <summary>
        /// Sets the Character set tag in the incoming DicomObject.
        /// </summary>
        /// <param name="dicomObject"></param>
        /// <param name="charSet"></param>
        internal static void SetCharSetValue(DicomObject dicomObject, string charSet)
        {
            if (!string.IsNullOrWhiteSpace(charSet))
            {
                if (charSet.Contains("\\"))
                {
                    dicomObject.SetStringArray(
                        DicomDictionary.DicomSpecificCharacterSet,
                        charSet.Split('\\'));
                }
                else
                {
                    dicomObject.SetString(
                        DicomDictionary.DicomSpecificCharacterSet, charSet);
                }
            }
        }

        /// <summary>
        /// Returns true if the given path is root drive
        /// </summary>
        internal static bool IsPathRootDrive(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            // During ConfigurationDump tests, some variants/deployments contain dummy values
            // which will be replaced during real installation.
            // However the test needs to pass in Developer/NUnit setup.
            if (
                path.Contains("__DATABASE_HOST_UNDEFINED__") ||
                path.StartsWith("\\", StringComparison.OrdinalIgnoreCase)
            )
            {
                return false;
            }

            var directoryInfo = new DirectoryInfo(path);
            return directoryInfo.FullName == directoryInfo.Root.FullName;
        }

        /// <summary>
        /// Check and throw device configuration exception if given path is root drive
        /// </summary>
        internal static void LogAndThrowExceptionIfPathIsRootDrive(string path)
        {
            if (IsPathRootDrive(path))
            {
                var errorMessage = "Configuration is invalid. Configured path \"" +
                    path.Anonymize() + "\" cannot be root drive.";
                LogError(errorMessage);
                throw new DeviceConfigurationException(errorMessage);
            }
        }

        /// <summary>
        /// Gets the drive type of the given path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>DriveType of path</returns>
        internal static DriveType GetPathDriveType(string path)
        {
            //OK, so UNC paths aren't 'drives', but this is still handy
            if (path.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
            {
                return DriveType.Network;
            }
            var info = DriveInfo.GetDrives().FirstOrDefault(
                i => path.StartsWith(i.Name, StringComparison.OrdinalIgnoreCase));
            if (info == null) return DriveType.Unknown;
            return info.DriveType;
        }


        /// <summary>
        /// Gets the filtered devices.
        /// </summary>
        /// <param name="sourceDeviceId">The source device id.</param>
        /// <param name="targetSubSystemName">Name of the target sub system.</param>
        /// <param name="targetDevices">The target devices.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// sourceDeviceId is null or empty
        /// or
        /// targetSubSystemName is null or empty
        /// or
        /// targetDevices is null
        /// </exception>
        internal static Tuple<string, IList<StorageDevice>> GetFilteredDevices(
            string sourceDeviceId,
            string targetSubSystemName,
            IList<StorageDevice> targetDevices)
        {

            if (string.IsNullOrEmpty(sourceDeviceId))
            {
                throw new ArgumentNullException("sourceDeviceId");
            }
            if (string.IsNullOrEmpty(targetSubSystemName))
            {
                throw new ArgumentNullException("targetSubSystemName");
            }
            if (targetDevices == null)
            {
                throw new ArgumentNullException("targetDevices");
            }

            //if Central Database does not exists return the target devices,
            // if target device is empty return the empty list
            if (
                (!DataServerManifestReader.DoesCentralDataServerExists && !IsTesting) ||
                targetDevices.Count == 0
            )
            {
                return Tuple.Create(string.Empty, targetDevices);
            }
            IList<StorageDevice> exportDevices = new List<StorageDevice>();
            string sourceDeviceSubSystem = string.Empty;
            var sourceDeviceLocation =
                DeviceConfigurationProxyManager.GetServiceLocationFor(sourceDeviceId);
            switch (targetSubSystemName)
            {
                case DataServerManifestReader.DataServerManifestName:
                    if (sourceDeviceLocation == Utilities.ServiceLocation.Local)
                    {
                        sourceDeviceSubSystem = DataServerManifestReader.DataServerManifestName;
                        //source device is client return all local devices
                        //except network devices.
                        foreach (var storageDevice in targetDevices)
                        {
                            if (storageDevice.Type != DeviceType.Network)
                            {
                                exportDevices.Add(storageDevice);
                            }
                        }
                    }
                    else if (sourceDeviceLocation == Utilities.ServiceLocation.Central)
                    {
                        sourceDeviceSubSystem =
                            DataServerManifestReader.CentralDataServerManifestName;
                        //when source device is Central return empty local devices.
                        exportDevices = new List<StorageDevice>();
                    }
                    break;
                case DataServerManifestReader.MediaManifestName:
                    if (sourceDeviceLocation == Utilities.ServiceLocation.Local)
                    {
                        sourceDeviceSubSystem = DataServerManifestReader.DataServerManifestName;
                        //if source device is local, return all media devices.
                        exportDevices = targetDevices;
                    }
                    else if (sourceDeviceLocation == Utilities.ServiceLocation.Central)
                    {
                        sourceDeviceSubSystem =
                            DataServerManifestReader.CentralDataServerManifestName;
                        //if source device is CDB, return empty media devices.
                        exportDevices = new List<StorageDevice>();
                    }
                    break;
                case DataServerManifestReader.CentralDataServerManifestName:
                    if (sourceDeviceLocation == Utilities.ServiceLocation.Local)
                    {
                        sourceDeviceSubSystem = DataServerManifestReader.DataServerManifestName;
                        //if source device is local, return only CDB local devices.
                        foreach (var storageDevice in targetDevices)
                        {
                            if (storageDevice.IsPrimaryDevice)
                            {
                                exportDevices.Add(storageDevice);
                                break;
                            }
                        }
                    }
                    else if (sourceDeviceLocation == Utilities.ServiceLocation.Central)
                    {
                        sourceDeviceSubSystem =
                            DataServerManifestReader.CentralDataServerManifestName;
                        //if source device is CDB, return all network devices.
                        foreach (var storageDevice in targetDevices)
                        {
                            if (storageDevice.Type == DeviceType.Network)
                            {
                                exportDevices.Add(storageDevice);
                            }
                        }
                    }
                    break;

                default:
                    //for all other subsystems, return all the devices 
                    exportDevices = targetDevices;
                    break;
            }
            return Tuple.Create(sourceDeviceSubSystem, exportDevices);
        }

        internal static bool IsTesting { get; set; }

        /// <summary>
        /// Check if Master-Slave Setup
        /// </summary>
        /// <returns></returns>
        internal static bool IsMasterSlaveSetUp()
        {
            bool isMasterSlaveSetup = false;
            if (DataServerUtility.TestPurpose)
            {
                return true;
            }
            if (
                !DataServerManifestReader.DoesCentralDataServerExists &&
                ConfigurationManager.Instance != null &&
                ConfigurationManager.Instance.RemoteSettings != null &&
                ConfigurationManager.Instance.RemoteSettings.Enabled
                )
            {
                var serviceUri = new Uri(ConfigurationManager.Instance.RemoteSettings.ServiceUrl);
                isMasterSlaveSetup = !DataServerUtility.IsServiceRunningLocal(serviceUri);
            }
            return isMasterSlaveSetup;
        }

        /// <summary>
        /// Checks if the device is a local device
        /// </summary>
        internal static bool IsLocalDevice(string deviceId)
        {
            return deviceId == ConfigurationManager.Instance.GetPrimaryDatabaseDeviceId() &&
                !ConfigurationManager.Instance.IsRemoteDatabase();
        }

        /// <summary>
        /// Converts QueryLevel to Level
        /// </summary>
        internal static Level ConvertToLevel(QueryLevel queryLevel)
        {
            switch (queryLevel)
            {
                case QueryLevel.Patient:
                    return Level.Patient;
                case QueryLevel.Study:
                    return Level.Study;
                case QueryLevel.Series:
                    return Level.Series;
                case QueryLevel.Image:
                    return Level.Image;
                case QueryLevel.Frame:
                    return Level.Frame;
                default:
                    var message = string.Format(CultureInfo.InvariantCulture,
                        "The conversion of querylevel {0} is not supported", queryLevel);
                    throw new ArgumentOutOfRangeException(message);
            }
        }

        /// <summary>
        /// Update completed column in Database.
        /// </summary>
        /// <param name="storageKey"></param>
        /// <param name="level">Study level or Series Level</param>
        /// <param name="value"></param>
        internal static void UpdateCompletedColumnInDatabase(
            StorageKey storageKey, Level level, int value)
        {
            var dbStore = DatabaseStoreBase.For(storageKey.SourceDevice);
            if (level == Level.Study)
            {
                dbStore.UpdateColumn(
                    Level.Study,
                    storageKey.Identifier.StudyInstanceUid,
                    StorageDevices.Constants.CompletedColumn,
                    value);
            }
            else if (level == Level.Series)
            {
                dbStore.UpdateColumn(
                    Level.Series,
                    storageKey.Identifier.SeriesInstanceUid,
                    StorageDevices.Constants.CompletedColumn,
                    value,
                    storageKey.Identifier.StudyInstanceUid);
            }
        }

        /// <summary>
        /// Updates the specified column with the corresponding value in Database
        /// </summary>
        /// <param name="level"></param>
        /// <param name="deviceId"></param>
        /// <param name="instanceUid"></param>
        /// <param name="columnName"></param>
        /// <param name="value"></param>
        /// <param name="parentInstanceUid"></param>
        internal static void UpdateColumnInDatabase(
            Level level,
            string deviceId,
            string instanceUid,
            string columnName,
            object value,
            string parentInstanceUid)
        {
            var dbStore = DatabaseStoreBase.CreateOnly(deviceId);
            dbStore.UpdateColumn(level, instanceUid, columnName, value, parentInstanceUid);
        }

        internal static int GetChildrenCountAtSeriesLevel(StorageKey seriesStoragekey)
        {
            var storageDevice = StorageDeviceProvider.GetDevice(seriesStoragekey.SourceDevice);

            int instanceCount = 0;
            if (storageDevice == null)
            {
                LogMessage("Failed to get device for " + seriesStoragekey.SourceDevice);
                return instanceCount;
            }

            instanceCount = storageDevice.GetChildrenCount(
                QueryLevel.Image, seriesStoragekey.Identifier,
                QueryFilter.MatchAll());

            return instanceCount;
        }

        private static void UpdateSeriesStateForImageLevelExport(
            StorageKey seriesStorageKey, string seriesStatus)
        {
            SeriesStateManager.SetSeriesState(
                seriesStorageKey, PhilipsDictionary.PiimExportStatus, seriesStatus, null);
            StateAggregatorClient.Instance.UpdateStudyState(
                seriesStorageKey, StorageDevices.Constants.SetMediaSeriesState);
        }

        internal static IList<string> GetChildrenSopAtSeriesLevel(StorageKey seriesStorageKey)
        {
            IList<string> sopList = StorageRecordHelper.GetAllImagesInstanceUidUnderSeries(
                seriesStorageKey.Identifier, seriesStorageKey, seriesStorageKey.SourceDevice);
            return sopList;
        }

        /// <summary>
        /// Computes the export status of series and updates the
        /// series and study export status for image level export
        /// </summary>
        /// <param name="seriesStorageKey"></param>
        /// <param name="imageExportStatusHandler"></param>
        internal static void TryUpdateSeriesAndStudyExportStatusForImageLevelExport(
            StorageKey seriesStorageKey, IImageExportStatusHandler imageExportStatusHandler)
        {
            if (SDStoreHelper.IsSeriesUnderConstruction(seriesStorageKey) ||
                SDStoreHelper.IsSeriesUnderModification(seriesStorageKey))
            {
                tracer.TraceVerbose("Series is under construction/modification hence " +
                    "series export status update will not be done");
                return;
            }
            var seriesStatus = imageExportStatusHandler.ComputeExportStatusForSeries(seriesStorageKey);
            if (!string.IsNullOrEmpty(seriesStatus))
            {
                UpdateSeriesStateForImageLevelExport(seriesStorageKey, seriesStatus);
            }
        }


        /// <summary>
        /// Gets the SQLite connection string.
        /// </summary>
        /// <returns></returns>
        internal static string GetSqLiteConnectionString(bool foreignKeys = true)
        {
            //Journal mode set to "Write Ahead Log", enable connection pooling, Enable Synchronous mode as FULL
            // Refer : https://www.sqlite.org/pragma.html#pragma_synchronous
            return
                "foreign keys = " + foreignKeys +
                "; Pooling = True;Max Pool Size = 100;Journal Mode = Wal;Synchronous = NORMAL;";
        }
    }
}
