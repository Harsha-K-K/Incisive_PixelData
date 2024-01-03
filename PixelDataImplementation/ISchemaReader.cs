using Philips.Platform.Common;
using Philips.Platform.StorageDevicesClient;
using System.Collections.Generic;

namespace PixelDataImplementation
{
    internal interface ISchemaReader
    {
        /// <summary>
        /// Load the configuration
        /// </summary>
        void Load();

        /// <summary>
        /// Get fast access tags
        /// </summary>
        List<DictionaryTag> GetFastAccessTags(Level level);

        /// <summary>
        /// Get queryable tags
        /// </summary>
        List<DictionaryTag> GetQueryableTags(Level level);

        /// <summary>
        /// Get Index queryable
        /// </summary>
        List<DictionaryTag> GetIndexQueryable(Level level);

        /// <summary>
        /// Get list fo tags skipped during modification
        /// </summary>
        List<DictionaryTag> GetTagsToSkipModification(Level level);

        /// <summary>
        /// Get Index queryable
        /// </summary>
        List<DictionaryTag> GetDicomDirTags(Level level);

        /// <summary>
        /// GetSopClassUids
        /// </summary>
        List<string> GetSopClassUids();
        /// <summary>
        /// Get SIDS attributes
        /// </summary>
        Dictionary<string, string> GetSidsAttributes();

        /// <summary>
        /// Get database service config
        /// </summary>
        Dictionary<string, string> GetDatabaseServiceConfig();
        /// <summary>
        /// Get cleanup config
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> GetCleanupConfig();
    }
}