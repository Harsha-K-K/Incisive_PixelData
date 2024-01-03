using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;
using Philips.Platform.CommonUtilities.Tracing;
using Philips.Platform.StorageDevices.FastInMemory.Client;
using Philips.Platform.StorageDevices.FastInMemory.Device;
using Philips.Platform.StorageDevices.FastInMemory.Service;
using Philips.Platform.SystemIntegration;
using Philips.Platform.Utilities;

namespace PixelDataImplementation
{
    /// <summary>
    /// Class to expose common utility methods of fast in-memory repository.
    /// </summary>
    //@AdapterType: Thread
    internal static class FastInMemoryUtility
    {
        private static readonly Tracer tracer = Tracer.CreateTracer(typeof(FastInMemoryUtility));

        private static readonly object lockObject = new object();
        /// <summary>
        /// Flag indicating if fast in-memory repository is enabled.
        /// </summary>
        private static bool? isFastRepositoryEnabled;

        internal static bool SignalEventsForTest { get; set; } = false;

        /// <summary>
        /// Implementation of <see cref="IFastRepositoryServiceHelper"/>
        /// </summary>
        internal static IFastRepositoryServiceHelper FastRepositoryServiceHelper { get; set; } =
            new FastRepositoryServiceHelper();

        internal static readonly DictionaryTag[] MandatoryStudyTableTags = {
            DicomDictionary.DicomStudyInstanceUid,
            DicomDictionary.DicomPatientId,
            DicomDictionary.DicomPatientName
        };

        internal static readonly DictionaryTag[] MandatorySeriesTableTags = {
            DicomDictionary.DicomStudyInstanceUid,
            DicomDictionary.DicomSeriesInstanceUid
        };

        /// <summary>
        /// Determines if fast repository is enabled in current deployment.
        /// </summary>
        /// <returns><see cref="isFastRepositoryEnabled"/></returns>
        internal static bool IsFastRepositoryEnabled()
        {
            if (isFastRepositoryEnabled != null)
            {
                return (bool)isFastRepositoryEnabled;
            }
            lock (lockObject)
            {
                if (isFastRepositoryEnabled == null)
                {
                    isFastRepositoryEnabled =
                        IsFastRepositoryServiceMutexInitialized() || IsFastRepositoryServiceInstalled();
                }
                return (bool)isFastRepositoryEnabled;
            }
        }

        /// <summary>
        /// Raise run time helper events for tests, if Signal for events is enabled.
        /// </summary>
        /// <param name="eventName"></param>
        internal static void RaiseEventsForTests(string eventName)
        {
            if (SignalEventsForTest)
            {
                RuntimeTestHelper.SignalEvent(eventName, false);
            }
        }

        /// <summary>
        /// Resets the values to default from test cases.
        /// </summary>
        internal static void ResetForTest()
        {
            isFastRepositoryEnabled = null;
            FastRepositoryServiceHelper = new FastRepositoryServiceHelper();
        }

        internal static IList<DictionaryTag> GetStudyColumnList()
        {
            var studyColumnDictionaryTags = StudySeriesAttributeProvider.Instance.StudyQueryAttributes;
            foreach (DictionaryTag tag in MandatoryStudyTableTags)
            {
                if (!studyColumnDictionaryTags.Contains(tag))
                {
                    studyColumnDictionaryTags.Insert(0, tag);
                }
            }
            return studyColumnDictionaryTags;
        }

        internal static IList<DictionaryTag> GetSeriesColumnList()
        {
            var seriesColumnDictionaryTags = StudySeriesAttributeProvider.Instance.SeriesQueryAttributes;
            foreach (DictionaryTag tag in MandatorySeriesTableTags)
            {
                if (!seriesColumnDictionaryTags.Contains(tag))
                {
                    seriesColumnDictionaryTags.Insert(0, tag);
                }
            }
            return seriesColumnDictionaryTags;
        }

        internal static SqlConnection OpenSqlServerConnection(string connectionString)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            if (connection.State != ConnectionState.Open)
            {
                // http://msdn.microsoft.com/en-us/library/8xx3tyca(v=vs.110).aspx
                // Note that a severed connection can be detected only after 
                // attempting to communicate with the server
                SqlConnection.ClearPool(connection);
                try
                {
                    connection.Open();
                }
                catch (Exception e)
                {
                    // in test mode we trace a readable in production it is 
                    // garbled to protect security information
                    tracer.TraceInfo(
                        $"SQLConnection failed, DB= {connection.Database}, " +
                        $"connection string= {connection.ConnectionString.Anonymize()} " +
                        $"with exception message {e.Message}",
                        e
                    );
                    throw;
                }
            }
            return connection;
        }

        /// <summary>
        /// Raises study/series or image level DB events via SLE.
        /// </summary>
        internal static void RaiseEvents(
            EntityType entityType,
            StorageDeviceEventArgs eventType,
            StorageKey currentPatientStudy,
            DicomObject currentDicomObject = null,
            DicomObject modifiedDicomObject = null
        )
        {
            FastRepositoryEventPublisher.Instance.RaiseEventsInBackground(
                entityType,
                eventType,
                currentPatientStudy,
                currentDicomObject,
                modifiedDicomObject);
        }
        /// <summary>
        /// Checks if <see cref="FastRepoConstants.FastRepositoryStorageServiceMutexName"/> has been initialized.
        /// <remarks>This mutex gets initialized during fast repository service start-up.</remarks>
        /// </summary>
        private static bool IsFastRepositoryServiceMutexInitialized()
        {
            try
            {
                if (
                    Mutex.TryOpenExisting(
                        FastRepoConstants.FastRepositoryStorageServiceMutexName,
                        out var mutex
                    )
                )
                {
                    mutex.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                tracer.TraceInfo("Unable to determine if fast repository service mutex is initialized.", ex);
            }
            return false;
        }
        /// <summary>
        /// Checks if fast repository service is installed on current system.
        /// </summary>
        private static bool IsFastRepositoryServiceInstalled()
        {
            try
            {
                return FastRepositoryServiceHelper.IsFastRepositoryServiceInstalled();
            }
            catch (Exception ex)
            {
                tracer.TraceInfo("Unable to retrieve installed services information.", ex);
                return false;
            }
        }
    }
}
