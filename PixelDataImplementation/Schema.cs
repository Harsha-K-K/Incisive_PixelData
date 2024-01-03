using Philips.Platform.Common;
using Philips.Platform.StorageDevicesClient;
using System.Collections.Generic;

namespace PixelDataImplementation
{
    /// <summary>
    /// Defines the schema for database.
    /// </summary>
    /// <remarks>
    /// This class has a collection of
    /// queryable and storable columns. <see cref="IDatabaseStore" /> 
    /// types use collections in this class for creating tables.
    /// </remarks>
    internal class Schema
    {

        private const int numberOfLevels = 5;

        private readonly Dictionary<Level, List<DictionaryTag>> queryables =
            new Dictionary<Level, List<DictionaryTag>>(numberOfLevels);

        private readonly Dictionary<Level, List<DictionaryTag>> fastAccess =
            new Dictionary<Level, List<DictionaryTag>>(numberOfLevels);

        private readonly Dictionary<Level, List<DictionaryTag>> IndexQuerable =
            new Dictionary<Level, List<DictionaryTag>>(numberOfLevels);

        /// <summary>
        /// Adds/Updates the given DictionaryTags in queryables dictionary
        /// </summary>
        /// <param name="level">the schema level</param>
        /// <param name="dictionaryTags">the DictionaryTags to be added</param>
        public void AddQueryable(Level level, params DictionaryTag[] dictionaryTags)
        {
            AddItems(queryables, level, dictionaryTags);
        }

        /// <summary>
        /// Adds/Updates the given DictionaryTags in fastaccess dictionary
        /// </summary>
        /// <param name="level">the schema level</param>
        /// <param name="dictionaryTags">the DictionaryTags to be added</param>
        public void AddFastAccess(Level level, params DictionaryTag[] dictionaryTags)
        {
            AddItems(fastAccess, level, dictionaryTags);
        }
        /// <summary>
        /// Adds/Updates the given DictionaryTags for indexable tags in dictionary
        /// </summary>
        /// <param name="level">the schema level</param>
        /// <param name="dictionaryTags">the DictionaryTags to be added</param>
        public void AddIndexQuerable(Level level, params DictionaryTag[] dictionaryTags)
        {
            AddItems(IndexQuerable, level, dictionaryTags);
        }


        /// <summary>
        /// Gets the queryables at given level
        /// </summary>
        /// <param name="level">the schema level</param>
        /// <returns>list of DictionaryTags at given level</returns>
        public List<DictionaryTag> QueryablesAt(Level level)
        {
            List<DictionaryTag> dicTags;
            queryables.TryGetValue(level, out dicTags);
            return dicTags;
        }

        /// <summary>
        /// Gets the fastaccess at given level
        /// </summary>
        /// <param name="level">the schema level</param>
        /// <returns>list of DictionaryTags at given level</returns>
        public List<DictionaryTag> FastAccessAt(Level level)
        {
            List<DictionaryTag> dicTags;
            fastAccess.TryGetValue(level, out dicTags);
            return dicTags;
        }

        /// <summary>
        /// Gets the Index querable attribute at given level
        /// </summary>
        /// <param name="level">the schema level</param>
        /// <returns>list of DictionaryTags at given level</returns>
        public List<DictionaryTag> IndexQueryableAt(Level level)
        {
            List<DictionaryTag> dicTags;
            IndexQuerable.TryGetValue(level, out dicTags);
            return dicTags;
        }

        private static void AddItems(
            Dictionary<Level, List<DictionaryTag>> itemToBeUpdated,
            Level level,
            params DictionaryTag[] dictionaryTags
        )
        {
            List<DictionaryTag> currentDictionaryTags;
            if (!itemToBeUpdated.TryGetValue(level, out currentDictionaryTags))
            {
                currentDictionaryTags = new List<DictionaryTag>();
            }
            currentDictionaryTags.AddRange(dictionaryTags);
            itemToBeUpdated[level] = currentDictionaryTags;
        }
    }
}