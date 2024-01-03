using Philips.Platform.Common;
using Philips.Platform.StorageDevicesClient;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace PixelDataImplementation
{
    /// <summary>
    /// DB SchemaChanges contain schema differences"/>
    /// </summary>
    internal class ColumnDetails
    {
        private string colunmName;
        private string columnType;
        private string displayName;
        private bool indexed = false;
        internal ColumnDetails(string displayName, string columnName, string columnType)
        {
            this.displayName = displayName;
            this.colunmName = columnName;
            this.columnType = columnType;
        }
        internal string ColumnName
        {
            get { return colunmName; }
        }
        internal string ColunmType
        {
            get { return columnType; }
        }

        internal string DisplayName
        {
            get { return displayName; }
        }
        internal bool isIndexed
        {
            get { return indexed; }
            set { indexed = value; }
        }

    }

    /// <summary>
    /// DB SchemaChanges contain schema differences"/>
    /// </summary>
    internal class DBSchemaChanges
    {

        /// <summary>
        /// The column added to table in DB.
        /// </summary>
        private Dictionary<Level, List<DictionaryTag>> querableAddedColumnlist =
            new Dictionary<Level, List<DictionaryTag>>();
        /// <summary>
        /// The column Deleted from table in DB.
        /// </summary>
        private Dictionary<Level, List<DictionaryTag>> querableDeletedColumnlist =
            new Dictionary<Level, List<DictionaryTag>>();

        /// <summary>
        /// In storable column tags added in DB ( its storabel column).
        /// </summary>
        private Dictionary<Level, List<DictionaryTag>> storableAddedColumnlist =
            new Dictionary<Level, List<DictionaryTag>>();


        /// <summary>
        /// In storable column tags added in DB ( its storabel column).
        /// </summary>
        private Dictionary<Level, List<DictionaryTag>> storableDeletedColumnlist =
            new Dictionary<Level, List<DictionaryTag>>();

        /// <summary>
        /// The Index added to table in DB.
        /// </summary>
        private Dictionary<Level, List<DictionaryTag>> addedIndexlist =
            new Dictionary<Level, List<DictionaryTag>>();

        /// <summary>
        /// The Index added to table in DB.
        /// </summary>
        private Dictionary<Level, List<DictionaryTag>> deletedIndexlist =
            new Dictionary<Level, List<DictionaryTag>>();


        internal void AddQuerableColumn(Level tableLevel, DictionaryTag tag)
        {
            if (!querableAddedColumnlist.ContainsKey(tableLevel))
            {
                List<DictionaryTag> tagList = new List<DictionaryTag>();
                tagList.Add(tag);
                querableAddedColumnlist[tableLevel] = tagList;
            }
            else
            {
                querableAddedColumnlist[tableLevel].Add(tag);
            }
        }

        internal void RemoveQuerableColumn(Level tableLevel, DictionaryTag tag)
        {
            if (!querableDeletedColumnlist.ContainsKey(tableLevel))
            {
                List<DictionaryTag> tagList = new List<DictionaryTag>();
                tagList.Add(tag);
                querableDeletedColumnlist[tableLevel] = tagList;
            }
            else
            {
                querableDeletedColumnlist[tableLevel].Add(tag);
            }
        }

        internal List<DictionaryTag> GetAddedQuerableColumnList(Level tableLevel)
        {
            if (querableAddedColumnlist.ContainsKey(tableLevel))
            {
                return querableAddedColumnlist[tableLevel];
            }
            return null;
        }

        internal List<DictionaryTag> GetRemovedQuerableColumnList(Level tableLevel)
        {
            if (querableDeletedColumnlist.ContainsKey(tableLevel))
            {
                return querableDeletedColumnlist[tableLevel];
            }
            return null;
        }

        internal void AddIndexableColumn(Level tableLevel, DictionaryTag tag)
        {
            if (!addedIndexlist.ContainsKey(tableLevel))
            {
                List<DictionaryTag> tagList = new List<DictionaryTag>();
                tagList.Add(tag);
                addedIndexlist[tableLevel] = tagList;
            }
            else
            {
                addedIndexlist[tableLevel].Add(tag);
            }
        }

        internal void RemoveIndexableColumn(Level tableLevel, DictionaryTag tag)
        {
            if (!deletedIndexlist.ContainsKey(tableLevel))
            {
                List<DictionaryTag> tagList = new List<DictionaryTag>();
                tagList.Add(tag);
                deletedIndexlist[tableLevel] = tagList;
            }
            else
            {
                deletedIndexlist[tableLevel].Add(tag);
            }
        }

        internal List<DictionaryTag> GetAddedIndexableColumnList(Level tableLevel)
        {
            if (addedIndexlist.ContainsKey(tableLevel))
            {
                return addedIndexlist[tableLevel];
            }
            return null;
        }
        internal List<DictionaryTag> GetRemovedIndexableColumnList(Level tableLevel)
        {
            if (deletedIndexlist.ContainsKey(tableLevel))
            {
                return deletedIndexlist[tableLevel];
            }
            return null;
        }

        internal void AddStorableColumn(Level tableLevel, DictionaryTag tag)
        {
            if (!storableAddedColumnlist.ContainsKey(tableLevel))
            {
                List<DictionaryTag> tagList = new List<DictionaryTag>();
                tagList.Add(tag);
                storableAddedColumnlist[tableLevel] = tagList;
            }
            else
            {
                storableAddedColumnlist[tableLevel].Add(tag);
            }
        }

        internal void RemoveStorableColumn(Level tableLevel, DictionaryTag tag)
        {
            if (!storableDeletedColumnlist.ContainsKey(tableLevel))
            {
                List<DictionaryTag> tagList = new List<DictionaryTag>();
                tagList.Add(tag);
                storableDeletedColumnlist[tableLevel] = tagList;
            }
            else
            {
                storableDeletedColumnlist[tableLevel].Add(tag);
            }
        }

        internal List<DictionaryTag> GetAddedStorableColumnList(Level tableLevel)
        {
            if (storableAddedColumnlist.ContainsKey(tableLevel))
            {
                return storableAddedColumnlist[tableLevel];
            }
            return null;
        }
        internal List<DictionaryTag> GetRemovedStorableColumnList(Level tableLevel)
        {
            if (storableDeletedColumnlist.ContainsKey(tableLevel))
            {
                return storableDeletedColumnlist[tableLevel];
            }
            return null;
        }

        internal bool isSchemaChanged()
        {
            if (
                querableAddedColumnlist.Count > 0 ||
                querableDeletedColumnlist.Count > 0 ||
                addedIndexlist.Count > 0 ||
                deletedIndexlist.Count > 0
                )
            {
                return true;
            }
            return false;
        }

        internal bool isFieldAddedOrModified()
        {
            if (
                querableAddedColumnlist.Count > 0 ||
                storableAddedColumnlist.Count > 0 ||
                storableDeletedColumnlist.Count > 0
                )
            {
                return true;
            }
            return false;
        }

        internal List<ColumnDetails> GetAddedColunm(Level level)
        {
            List<ColumnDetails> columnAdded = new List<ColumnDetails>();
            if (querableAddedColumnlist.ContainsKey(level))
            {
                List<DictionaryTag> dicTagList = querableAddedColumnlist[level];
                if (dicTagList != null)
                {
                    foreach (DictionaryTag dicTag in dicTagList)
                    {
                        columnAdded.Add(GetColumnDetails(level, dicTag));
                    }
                }
            }
            return columnAdded;
        }

        internal List<ColumnDetails> GetDeletedColunm(Level level)
        {
            List<ColumnDetails> columnRemoved = new List<ColumnDetails>();
            if (querableDeletedColumnlist.ContainsKey(level))
            {
                List<DictionaryTag> dicTagList = querableDeletedColumnlist[level];
                if (dicTagList != null)
                {
                    foreach (DictionaryTag dicTag in dicTagList)
                    {
                        columnRemoved.Add(GetColumnDetails(level, dicTag, false));
                    }
                }
            }
            return columnRemoved;
        }
        internal bool StorableColumnModified(Level level)
        {
            if (
                storableAddedColumnlist.ContainsKey(level) &&
                storableAddedColumnlist[level].Count > 0 ||
                storableDeletedColumnlist.ContainsKey(level) &&
                storableDeletedColumnlist[level].Count > 0
                )
            {
                return true;
            }
            return false;
        }
        private ColumnDetails GetColumnDetails
            (Level level, DictionaryTag dicTag, bool newColumn = true)
        {
            ColumnDetails details = new ColumnDetails(dicTag.Name,
                SqlCommandBuilder.GetColumnName(dicTag),
                SqlCommandBuilder.GetColumnType(dicTag));

            details.isIndexed = IsIndexed(level, dicTag, newColumn);
            return details;
        }

        private bool IsIndexed(Level level, DictionaryTag dicTag, bool newColumn = true)
        {
            bool bIndexed = false;
            if (newColumn)
            {
                if (addedIndexlist.ContainsKey(level) && addedIndexlist[level] != null)
                {
                    return addedIndexlist[level].Contains(dicTag);
                }
            }
            else
            {
                if (deletedIndexlist.ContainsKey(level) && deletedIndexlist[level] != null)
                {
                    return deletedIndexlist[level].Contains(dicTag);
                }
            }

            return bIndexed;
        }
    }
}