using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;
using Philips.Platform.SystemIntegration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// Event Type
    /// </summary>
    [Flags]
    internal enum EventType
    {

        /// <summary>
        /// Added
        /// </summary>
        Add = 1,

        /// <summary>
        /// Deleted
        /// </summary>
        Delete = 2,

        /// <summary>
        /// Modify
        /// </summary>
        Modify = 4,

        /// <summary>
        /// Dicom dir modified
        /// </summary>
        DicomDirModified = 8,
        /// <summary>
        /// 
        /// </summary>
        Completed = 16,
        /// <summary>
        /// Update
        /// </summary>
        Update = 32,

        /// <summary>
        /// Centralized Databse Study Added
        /// </summary>
        CdbStudyAdded = 64,

        /// <summary>
        /// Centralized Databse Study Deleted
        /// </summary>
        CdbStudyDeleted = 128,

        /// <summary>
        /// Centralized Databse Study Updated
        /// </summary>
        CdbStudyUpdated = 256,

        /// <summary>
        /// User selectable device updated
        /// </summary>
        DeviceUpdated = 512,

        /// <summary>
        /// DeviceCapabilityUpdated 
        /// </summary>
        DeviceCapabilityUpdated = 1024,

        /// <summary>
        /// DeviceLocked 
        /// </summary>
        DeviceLocked = 2048,

        /// <summary>
        /// State flag changed
        /// Currently being used for series state flag via SetSeriesState
        /// </summary>
        StateChanged = 4096
    }

    /// <summary>
    /// Level
    /// </summary>
    [Flags]

    internal enum EventLevels
    {
        /// <summary>
        /// Patient level
        /// </summary>
        Patient = 8,

        /// <summary>
        /// Study level
        /// </summary>
        Study = 16,

        /// <summary>
        /// Series level
        /// </summary>
        Series = 32,

        /// <summary>
        /// SF Image level
        /// </summary>
        SFImage = 64,

        /// <summary>
        /// MF Image level
        /// </summary>
        MFImage = 128,

        /// <summary>
        /// Blob level
        /// </summary>
        Blob = 256,

        /// <summary>
        /// Device level
        /// </summary>
        Device = 512,

        /// <summary>
        /// The configuration update
        /// </summary>
        ConfigurationUpdate = 1024
    }

    /// <summary>
    /// Holds system event details.
    /// </summary>
    [Serializable]
    [DataContract]
    internal class StorageDeviceEventArgs : EventArgs
    {
        private static readonly int currentProcessId = Process.GetCurrentProcess().Id;
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int ProcessId { get; private set; }

        /// <summary>
        /// Event Type
        /// </summary>
        [DataMember]
        public StorageDeviceEventArgs EventType { get; private set; }

        /// <summary>
        /// Level at which event occurred
        /// </summary>
        [DataMember]
        public EventLevels EventLevel { get; private set; }

        /// <summary>
        /// StorageKey to identify object where event occurred.
        /// </summary>
        [DataMember]
        public StorageKey StorageKey { get; private set; }

        /// <summary>
        /// Time stamp on when the event occurred
        /// </summary>
        [DataMember]
        public DateTime DateTime { get; private set; }

        /// <summary>
        /// Gets or sets the event index
        /// </summary>        
        [DataMember]
        public int EventIndex { get; set; }

        /// <summary>
        /// Gets or sets the state of the object before the event.
        /// </summary>
        [DataMember]
        public DicomObject PreviousState { get; private set; }

        /// <summary>
        /// Gets or sets the state of the object after the event.
        /// </summary>
        [DataMember]
        public DicomObject CurrentState { get; private set; }

        /// <summary>
        /// Gets or sets the name of the object after the event.
        /// </summary>
        [DataMember]
        public string ObjectName { get; private set; }

        /// <summary>
        /// List of updated series Uids
        /// </summary>
        [DataMember]
        public List<string> UpdatedSeriesUids { get; set; }

        /// <summary>
        /// UpdatedState
        /// </summary>
        [DataMember]
        public StudyUpdateState UpdatedState { get; set; }

        /// <summary>
        /// Source
        /// </summary>
        [DataMember]
        public ImportSource Source { get; set; }

        /// <summary>
        /// IsSourceRemote
        /// </summary>
        [DataMember]
        public bool IsSourceRemote { get; set; }
        /// <summary>
        /// Machine MAC Address
        /// </summary>
        [DataMember]
        public string MachineMacAddress { get; set; }

        /// <summary>
        /// Gets the collection of bulk data directory paths
        /// </summary>
        [DataMember]
        public List<string> BulkDataDirectoryPathCollection { get; private set; }

        /// <summary>
        /// Gets the series storage key collection
        /// </summary>
        [DataMember]
        public IEnumerable<StorageKey> SeriesKeyCollection { get; private set; }

        /// <summary>
        /// Sets the type of dicomdir modfication event
        /// Types are Store and Delete
        /// </summary>
        [DataMember]
        public DicomDirEventTypes DicomDirEventType { get; private set; }

        /// <summary>
        /// Sets the the device id
        /// </summary>
        [DataMember]
        public string DeviceId { get; private set; }

        /// <summary>
        /// Initializes StorageDeviceEventArgs class instance with specified values.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="level">The level.</param>
        /// <param name="key">The key.</param>
        /// <param name="dateTime">The date time.</param>
        public StorageDeviceEventArgs(
            StorageDeviceEventArgs type,
            EventLevels level,
            StorageKey key,
            DateTime dateTime
        )
        {
            EventType = type;
            EventLevel = level;
            StorageKey = key;
            DateTime = dateTime;
            ProcessId = currentProcessId;
        }

        /// <summary>
        /// Initializes StorageDeviceEventArgs class instance with specified values.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="level">The level.</param>
        /// <param name="key">The key.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="bulkDataDirectoryPathsCollection">
        /// bulk data directory path collection
        /// </param>
        public StorageDeviceEventArgs(
            StorageDeviceEventArgs type,
            EventLevels level,
            StorageKey key,
            DateTime dateTime,
            List<string> bulkDataDirectoryPathsCollection
        )
        {
            EventType = type;
            EventLevel = level;
            StorageKey = key;
            DateTime = dateTime;
            ProcessId = currentProcessId;
            BulkDataDirectoryPathCollection = bulkDataDirectoryPathsCollection;
        }

        /// <summary>
        /// Initializes StorageDeviceEventArgs class instance with specified values.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="level">The level.</param>
        /// <param name="key">The key.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="bulkDataDirectoryPathsCollection">
        /// bulk data directory path collection
        /// </param>
        /// <param name="seriesKeyCollection">series storage key collection.</param>
        public StorageDeviceEventArgs(
            StorageDeviceEventArgs type,
            EventLevels level,
            StorageKey key,
            DateTime dateTime,
            List<string> bulkDataDirectoryPathsCollection,
            List<StorageKey> seriesKeyCollection
        )
        {
            EventType = type;
            EventLevel = level;
            StorageKey = key;
            DateTime = dateTime;
            ProcessId = currentProcessId;
            BulkDataDirectoryPathCollection = bulkDataDirectoryPathsCollection;
            SeriesKeyCollection = seriesKeyCollection;
        }

        /// <summary>
        /// Initializes StorageDeviceEventArgs class instance with specified values.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="level">The level.</param>
        /// <param name="key">The key.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="objectName"> object name after the event</param>
        public StorageDeviceEventArgs(
            StorageDeviceEventArgs type,
            EventLevels level,
            StorageKey key,
            DateTime dateTime,
            string objectName
        ) : this(type, level, key, dateTime)
        {
            ObjectName = objectName;
        }

        /// <summary>
        /// Initializes StorageDeviceEventArgs class instance wuth specified values.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="level">The level.</param>
        /// <param name="key">The key.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="previousState">The state of the object before the event.</param>
        /// <param name="currentState">The state of the object after the event.</param>
        /// <param name="state">The StudyUpdateState of the affected.</param>
        /// <param name="changedUids">The Instance Uids of the affected.</param>
        /// <param name="ImportSrc">The Source of Import</param>
        /// <param name="IsRemote">IsRemote</param>
        public StorageDeviceEventArgs(
            StorageDeviceEventArgs type,
            EventLevels level,
            StorageKey key,
            DateTime dateTime,
            DicomObject previousState,
            DicomObject currentState,
            StudyUpdateState state = StudyUpdateState.None,
            List<string> changedUids = null,
            ImportSource ImportSrc = ImportSource.PDImport,
            bool IsRemote = false
            ) :
            this(type, level, key, dateTime)
        {

            FillStates(previousState, currentState);
            UpdatedSeriesUids = changedUids;
            UpdatedState = state;
            this.Source = ImportSrc;
            IsSourceRemote = IsRemote;
        }


        /// <summary>
        /// Initializes StorageDeviceEventArgs class instance with
        /// specified values for CDB modification events.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="level">The level.</param>
        /// <param name="key">The key.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="previousState">The state of the object before the event.</param>
        /// <param name="currentState">The state of the object after the event.</param>
        /// <param name="hostMacAddress"/>
        /// <param name="state">The StudyUpdateState of the affected.</param>
        /// <param name="changedUids">The Instance Uids of the affected.</param>
        /// <param name="ImportSrc">The Source of Import</param>
        /// <param name="IsRemote">IsRemote</param>
        public StorageDeviceEventArgs(
            StorageDeviceEventArgs type,
            EventLevels level,
            StorageKey key,
            DateTime dateTime,
            DicomObject previousState,
            DicomObject currentState,
            string hostMacAddress,
            StudyUpdateState state = StudyUpdateState.None,
            List<string> changedUids = null,
            ImportSource ImportSrc = ImportSource.PDImport,
            bool IsRemote = false
        ) : this(type, level, key, dateTime)
        {
            FillStates(previousState, currentState);
            UpdatedSeriesUids = changedUids;
            UpdatedState = state;
            this.Source = ImportSrc;
            IsSourceRemote = IsRemote;
            MachineMacAddress = hostMacAddress;
        }

        /// <summary>
        /// Initializes StorageDeviceEventArgs class instance wuth specified values.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="level">The level.</param>
        /// <param name="key">The key.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="previousState">The state of the object before the event.</param>
        /// <param name="currentState">The state of the object after the event.</param>
        /// <param name="state">The StudyUpdateState of the affected.</param>
        /// <param name="changedUids">The Instance Uids of the affected.</param>
        /// <param name="ImportSrc">The Source of Import</param>
        /// <param name="IsRemote">IsRemote</param>
        /// <param name="bulkDataDirectoryPathsCollection">
        /// bulk date directory path collection
        /// </param>
        public StorageDeviceEventArgs(
            StorageDeviceEventArgs type,
            EventLevels level,
            StorageKey key,
            DateTime dateTime,
            DicomObject previousState,
            DicomObject currentState,
            List<string> bulkDataDirectoryPathsCollection,
            StudyUpdateState state = StudyUpdateState.None,
            List<string> changedUids = null,
            ImportSource ImportSrc = ImportSource.PDImport,
            bool IsRemote = false
            ) :
            this(type, level, key, dateTime)
        {

            FillStates(previousState, currentState);
            UpdatedSeriesUids = changedUids;
            UpdatedState = state;
            this.Source = ImportSrc;
            IsSourceRemote = IsRemote;
            BulkDataDirectoryPathCollection = bulkDataDirectoryPathsCollection;
        }

        /// <summary>
        /// Initializes StorageDeviceEventArgs class instance wuth specified values.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="level">The level.</param>
        /// <param name="key">The key.</param>
        /// <param name="dateTime">The date time.</param>
        /// <param name="previousState">The state of the object before the event.</param>
        /// <param name="currentState">The state of the object after the event.</param>
        /// <param name="state">The StudyUpdateState of the affected.</param>
        /// <param name="changedUids">The Instance Uids of the affected.</param>
        /// <param name="ImportSrc">The Source of Import</param>
        /// <param name="IsRemote">IsRemote</param>
        ///<param name="seriesKeyCollection">Series storage key collection</param>
        /// <param name="bulkDataDirectoryPathsCollection">
        /// bulk date directory path collection
        /// </param>
        public StorageDeviceEventArgs(
            StorageDeviceEventArgs type,
            EventLevels level,
            StorageKey key,
            DateTime dateTime,
            DicomObject previousState,
            DicomObject currentState,
            List<string> bulkDataDirectoryPathsCollection,
            List<StorageKey> seriesKeyCollection,
            StudyUpdateState state = StudyUpdateState.None,
            List<string> changedUids = null,
            ImportSource ImportSrc = ImportSource.PDImport,
            bool IsRemote = false
        ) :
            this(type, level, key, dateTime)
        {

            FillStates(previousState, currentState);
            UpdatedSeriesUids = changedUids;
            UpdatedState = state;
            this.Source = ImportSrc;
            IsSourceRemote = IsRemote;
            BulkDataDirectoryPathCollection = bulkDataDirectoryPathsCollection;
            SeriesKeyCollection = seriesKeyCollection;
        }

        /// <summary>
        /// Initializes StorageDeviceEventArgs class instance wuth specified values.
        /// </summary>
        /// <param name="type">Event type</param>
        /// <param name="deviceId">Device Id</param>
        /// <param name="dicomDirEventType"> Dicomdir modification event type</param>
        /// <param name="dateTime">Time of the modification event</param>
        public StorageDeviceEventArgs(
            StorageDeviceEventArgs type,
            string deviceId,
            DicomDirEventTypes dicomDirEventType,
            DateTime dateTime
        )
        {
            EventType = type;
            DeviceId = deviceId;
            DateTime = dateTime;
            DicomDirEventType = dicomDirEventType;
            ProcessId = currentProcessId;
        }

        private void FillStates(DicomObject previousState, DicomObject currentState)
        {
            if (previousState != null)
            {
                var aipDicomObject = (Dicom.Data.DicomObject)DicomObject.CreateInstance();
                aipDicomObject.CombineWithDeepCopy((Dicom.Data.DicomObject)previousState);
                PreviousState = aipDicomObject;
            }
            if (currentState != null)
            {
                var aipDicomObject = (Dicom.Data.DicomObject)DicomObject.CreateInstance();
                aipDicomObject.CombineWithDeepCopy((Dicom.Data.DicomObject)currentState);
                CurrentState = aipDicomObject;
            }
        }
    }
#pragma warning restore TI7107 // Limit the contents of a source code file to one type

}
