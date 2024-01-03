using Philips.Platform.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    // @AdapterType: Wrapper
    /// <summary>
    /// Class to raise Fast Repository DB events. 
    /// </summary>
    internal class FastRepositoryEventPublisher
    {

        private Func<string, StorageDeviceEventArgs, bool, bool> publishEventHandler;
        internal static FastRepositoryEventPublisher Instance { get; } = new FastRepositoryEventPublisher();

        private FastRepositoryEventPublisher()
        {
        }

        internal void InjectPublishHandler(Func<string, StorageDeviceEventArgs, bool, bool> eventHandler)
        {
            publishEventHandler = eventHandler;
        }

        /// <summary>
        /// Raise data modified Events for fast repo.
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="eventType"></param>
        /// <param name="storageKey"></param>
        /// <param name="originalDicomObject">Original dicom object before update.</param>
        /// <param name="modifiedDicomObject">Modified dicom object after update.</param>
        //TICS -6@201 cyclomatic complexity. The code is fairly readable
        internal void RaiseEventsInBackground(
            EntityType entityType,
            StorageDeviceEventArgs eventType,
            StorageKey storageKey,
            DicomObject originalDicomObject = null,
            DicomObject modifiedDicomObject = null
        )
        {
            if (entityType == EntityType.Study)
            {
                switch (eventType)
                {
                    case StorageDeviceEventArgs.Add:
                        RaiseStudyAddedEvent(storageKey);
                        break;
                    case StorageDeviceEventArgs.Delete:
                        RaiseStudyDeletedEvent(storageKey);
                        break;
                    case StorageDeviceEventArgs.Modify:
                        RaiseStudyModifiedEvent(storageKey, originalDicomObject, modifiedDicomObject);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
                }
            }
            else if (entityType == EntityType.Series)
            {
                switch (eventType)
                {
                    case StorageDeviceEventArgs.Add:
                        RaiseSeriesAddedEvent(storageKey);
                        break;
                    case StorageDeviceEventArgs.Modify:
                        RaiseSeriesModifyEvent(storageKey);
                        break;
                    case StorageDeviceEventArgs.Delete:
                        RaiseSeriesDeletedEvent(storageKey);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
                }
            }
            else if (entityType == EntityType.SFImage)
            {
                if (eventType == StorageDeviceEventArgs.Add)
                {
                    RaiseSfImageAddedEvent(storageKey);
                }
            }
        }
        //TICS +6@201 cyclomatic complexity.
        private void RaiseStudyAddedEvent(StorageKey patientStudyStorageKey)
        {
            var eventId = patientStudyStorageKey.SourceDevice + EventLevels.Study + StorageDeviceEventArgs.Add;
            var args = new StorageDeviceEventArgs(
                StorageDeviceEventArgs.Add, EventLevels.Study, patientStudyStorageKey, DateTime.Now
            );
            publishEventHandler(eventId, args, false);
        }

        private void RaiseSeriesAddedEvent(StorageKey seriesStorageKey)
        {
            var eventId = seriesStorageKey.SourceDevice + EventLevels.Series + StorageDeviceEventArgs.Add;
            var args = new StorageDeviceEventArgs(
                StorageDeviceEventArgs.Add, EventLevels.Series, seriesStorageKey, DateTime.Now
            );
            publishEventHandler(eventId, args, false);
        }

        private void RaiseSeriesModifyEvent(StorageKey seriesStorageKey)
        {
            var eventId = seriesStorageKey.SourceDevice + EventLevels.Series + StorageDeviceEventArgs.Modify;
            var args = new StorageDeviceEventArgs(
                StorageDeviceEventArgs.Modify, EventLevels.Series, seriesStorageKey, DateTime.Now
            );
            publishEventHandler(eventId, args, false);
        }

        private void RaiseSfImageAddedEvent(StorageKey sfImageStorageKey)
        {
            var eventId = sfImageStorageKey.SourceDevice + EventLevels.SFImage + StorageDeviceEventArgs.Add;
            var args = new StorageDeviceEventArgs(
                StorageDeviceEventArgs.Add, EventLevels.SFImage, sfImageStorageKey, DateTime.Now
            );
            publishEventHandler(eventId, args, false);
        }

        private void RaiseStudyDeletedEvent(StorageKey studyStorageKey)
        {
            var eventId = studyStorageKey.SourceDevice + EventLevels.Study + StorageDeviceEventArgs.Delete;
            var args = new StorageDeviceEventArgs(
                StorageDeviceEventArgs.Delete, EventLevels.Study, studyStorageKey, DateTime.Now
            );
            publishEventHandler(eventId, args, false);
        }

        private void RaiseSeriesDeletedEvent(StorageKey seriesStorageKey)
        {
            var eventId = seriesStorageKey.SourceDevice + EventLevels.Series + StorageDeviceEventArgs.Delete;
            var args = new StorageDeviceEventArgs(
                StorageDeviceEventArgs.Delete, EventLevels.Series, seriesStorageKey, DateTime.Now
            );
            publishEventHandler(eventId, args, false);
        }

        private void RaiseStudyModifiedEvent(
            StorageKey studyStorageKey, DicomObject currentStudy, DicomObject modifiedStudy)
        {
            var eventId = studyStorageKey.SourceDevice + EventLevels.Study + StorageDeviceEventArgs.Modify;
            var storageDeviceEventArgs = new StorageDeviceEventArgs(
                StorageDeviceEventArgs.Modify,
                EventLevels.Study,
                studyStorageKey,
                DateTime.Now,
                currentStudy,
                modifiedStudy);
            publishEventHandler(eventId, storageDeviceEventArgs, false);
        }
    }
}
