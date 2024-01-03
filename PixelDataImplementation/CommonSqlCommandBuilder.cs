using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;
using Philips.Platform.StorageDevicesClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// Helper class to Build Sql Command
    /// </summary>
    internal class CommonSqlCommandBuilder
    {

        private readonly DevelopmentLogger logger;
        /// <summary>
        /// Initializes a new instance of the <see cref="CommonSqlCommandBuilder"/> class.
        /// </summary>
        internal CommonSqlCommandBuilder(
            DatabaseType typeOfDatabase,
            List<DictionaryTag> patientQueryableTags,
            List<DictionaryTag> studyQueryableTags,
            List<DictionaryTag> seriesQueryableTags
        )
        {
            logger = new DevelopmentLogger(GetType().Name, GetType().Namespace);

            databaseType = typeOfDatabase;
            patientQueryables = patientQueryableTags;
            studyQueryables = studyQueryableTags;
            seriesQueryables = seriesQueryableTags;
        }

        private int indexer;

        private readonly DatabaseType databaseType;

        private readonly List<DictionaryTag> studyQueryables;

        private readonly List<DictionaryTag> seriesQueryables;

        private readonly List<DictionaryTag> patientQueryables;

        internal const string AscendingOrderCommandText = " ASC";

        internal const string DescendingOrderCommandText = " DESC";

        /// <summary>
        /// Populates the given <see cref="IDbCommand" /> with CommandText and
        /// parameter collection based on the input <see cref="QueryFilter" />
        /// and <see cref="Identifier" />
        /// </summary>
        /// <param name="sqlCommand">The sql command to be updated</param>
        /// <param name="deviceId">The device id</param>
        /// <param name="level">the database query level</param>
        /// <param name="parentIdentifier">the parent identifier</param>
        /// <param name="filter">the query filter array</param>
        /// <param name="sortCriteria">Collection of Dictionary tags to be used for sorting
        /// purpose.</param>
        /// <param name="sortOrder">Determines what is the sort order.</param>
        /// <param name="maxRecords">Limits the maximum number of results returned by query.</param>
        /// <param name="returnAllColumns">flag to decide whether to select all columns
        /// or only storables in the query command</param>
        /// <param name="onlyCompleted">if set to <c>true</c> [only completed].</param>
        /// <param name="onlyInstanceUids">if set to <c>true</c> [only instance uids].</param>
        public void PopulateQueryCommand(
            IDbCommand sqlCommand,
            string deviceId,
            Level level,
            Identifier parentIdentifier,
            QueryFilter filter,
            DictionaryTagsCollection sortCriteria = null,
            QuerySortOrder sortOrder = QuerySortOrder.None,
            int maxRecords = 200,
            bool returnAllColumns = false,
            bool onlyCompleted = false,
            bool onlyInstanceUids = false
        )
        {
            indexer = 0;
            sqlCommand.Parameters.Clear();

            string deviceIdWhereClause =
                GetWhereClause(sqlCommand, Constants.DeviceIdColumn, deviceId);

            string parentKeyWhereClause =
                GetParentKeyWhereClause(level, parentIdentifier, sqlCommand);

            List<DictionaryTag> definedTags = GetQueryableTags(level);
            string whereClauses = GetWhereClause(filter, sqlCommand, definedTags);

            string finalWhereClause = deviceIdWhereClause;

            if (onlyCompleted)
            {
                sqlCommand.AddParameterWithValue("@completed", 1);
                string completedWhereClause = "[" + Constants.CompletedColumn + "]=@completed";
                finalWhereClause += " AND " + completedWhereClause;
            }

            //Build the complete where clause, which includes parent and filter clauses
            if (!string.IsNullOrWhiteSpace(parentKeyWhereClause))
            {
                finalWhereClause += " AND " + parentKeyWhereClause;
            }
            if (!string.IsNullOrWhiteSpace(whereClauses))
            {
                finalWhereClause += " AND " + whereClauses;
            }

            sqlCommand.CommandText =
                BuildCompleteCommand(level, sortCriteria, sortOrder,
                maxRecords, finalWhereClause, returnAllColumns, onlyInstanceUids);
        }

        /// <summary>
        /// Populates the given <see cref="IDbCommand"/> with CommandText and
        /// parameter collection based on the input <see cref="QueryFilter"/>
        /// and <see cref="Identifier"/>. This command will only return the count of the 
        /// matching records.
        /// </summary>
        public void PopulateCountCommand(
            IDbCommand sqlCommand,
            string deviceId,
            Level level,
            Identifier parentIdentifier,
            QueryFilter filter
        )
        {
            indexer = 0;
            sqlCommand.Parameters.Clear();

            string deviceIdWhereClause =
                GetWhereClause(sqlCommand, Constants.DeviceIdColumn, deviceId);

            string parentKeyWhereClause =
                GetParentKeyWhereClause(level, parentIdentifier, sqlCommand);

            List<DictionaryTag> definedTags = GetQueryableTags(level);
            string whereClauses = GetWhereClause(filter, sqlCommand, definedTags);

            string finalWhereClause = deviceIdWhereClause;

            //Build the complete where clause, which includes parent and filter clauses
            if (!string.IsNullOrWhiteSpace(parentKeyWhereClause))
            {
                finalWhereClause += " AND " + parentKeyWhereClause;
            }
            if (!string.IsNullOrWhiteSpace(whereClauses))
            {
                finalWhereClause += " AND " + whereClauses;
            }

            sqlCommand.CommandText = BuildCountCommand(level, finalWhereClause);
        }

        /// <summary>
        /// Builds final sql command text by adding all sub parts.
        /// </summary>
        private string BuildCompleteCommand(
            Level level,
            DictionaryTagsCollection sortCriteria,
            QuerySortOrder sortOrder,
            int maxRecords,
            string finalWhereClause,
            bool returnAllColumns,
            bool onlyInstanceUids
        )
        {
            StringBuilder stringBuilder = new StringBuilder(32);
            if (databaseType == DatabaseType.SqlServer && sortCriteria != null && maxRecords > 0)
            {
                stringBuilder.Append("SELECT TOP(").Append(maxRecords).Append(") storables FROM ");
            }
            else if (returnAllColumns)
            {
                stringBuilder.Append("SELECT * FROM ");
            }
            else if (onlyInstanceUids)
            {
                stringBuilder.Append("SELECT uid FROM ");
            }
            else
            {
                stringBuilder.Append("SELECT storables FROM ");
            }

            stringBuilder.Append(level);
            stringBuilder.Append(
                string.IsNullOrEmpty(finalWhereClause) ? "" : " WHERE " + finalWhereClause
            );

            string orderByClause = GetOrderByClause(level, sortCriteria, sortOrder);
            if (!string.IsNullOrWhiteSpace(orderByClause))
            {
                stringBuilder.Append(orderByClause);
            }

            if (databaseType == DatabaseType.Sqlite && sortCriteria != null && maxRecords > 0)
            {
                stringBuilder.Append(" LIMIT ").Append(maxRecords);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Builds a command to count the number of rows matching 
        /// the <paramref name="whereClause"/>.
        /// </summary>
        private static string BuildCountCommand(
            Level level,
            string whereClause
        )
        {
            StringBuilder stringBuilder = new StringBuilder(32);
            stringBuilder.Append("SELECT COUNT (*) FROM ");
            stringBuilder.Append(level);
            stringBuilder.Append(
                string.IsNullOrEmpty(whereClause) ? "" : " WHERE " + whereClause
            );
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Gets the order by clause statement from specified inputs.
        /// </summary>
        internal string GetOrderByClause(
            Level level,
            DictionaryTagsCollection sortCriteria,
            QuerySortOrder sortOrder
        )
        {
            var orderByClause = new StringBuilder(1000);
            var validTagsForSorting = new DictionaryTagsCollection();
            if (sortCriteria == null || sortCriteria.Count == 0)
            {
                return orderByClause.ToString();
            }

            if (!GetValidSortCriteria(level, sortCriteria, validTagsForSorting))
            {
                return orderByClause.ToString();
            }
            orderByClause.Append(" ORDER BY ");
            foreach (var item in validTagsForSorting)
            {
                if (
                    item.ValueRepresentation == DicomVR.IS ||
                    item.ValueRepresentation == DicomVR.SS ||
                    item.ValueRepresentation == DicomVR.US
                )
                {
                    orderByClause.Append("CAST(" + GetColumnName(item) + " AS INT), ");
                }
                else if (
                    item.ValueRepresentation == DicomVR.SL ||
                    item.ValueRepresentation == DicomVR.UL
                )
                {
                    orderByClause.Append("CAST(" + GetColumnName(item) + " AS BIGINT), ");
                }
                else
                {
                    orderByClause.Append(GetColumnName(item) + ", ");
                }
            }
            orderByClause.Remove(orderByClause.Length - 2, 2);

            if (sortOrder == QuerySortOrder.Ascending)
            {
                orderByClause.Append(AscendingOrderCommandText);
            }
            else if (sortOrder == QuerySortOrder.Descending)
            {
                orderByClause.Append(DescendingOrderCommandText);
            }
            return orderByClause.ToString();
        }

        /// <summary>
        /// Populates the given <see cref="IDbCommand"/> with CommandText and
        /// parameter collection based on the input <see cref="QueryFilter"/>
        /// and <see cref="Identifier"/>
        /// </summary>
        /// <param name="sqlCommand">The sql command to be updated</param>
        /// <param name="deviceId">The device id</param>
        /// <param name="level">the database query level</param>
        /// <param name="filter">the query filter array</param>
        public void PopulateQueryAllCommand(
            IDbCommand sqlCommand,
            string deviceId,
            Level level,
            QueryFilter filter
        )
        {
            indexer = 0;
            sqlCommand.Parameters.Clear();
            string deviceIdWhereClause =
                GetWhereClause(sqlCommand, Constants.DeviceIdColumn, deviceId);

            List<DictionaryTag> definedTags = GetQueryableTags(level);

            string whereClauses = GetWhereClause(filter, sqlCommand, definedTags);

            string finalWhereClause = deviceIdWhereClause;

            //Build the complete where clause, which includes parent and filter clauses

            if (!string.IsNullOrWhiteSpace(whereClauses))
            {
                finalWhereClause += " AND " + whereClauses;
            }

            StringBuilder stringBuilder = new StringBuilder(3);
            stringBuilder.Append("SELECT * FROM ");
            stringBuilder.Append(level);
            stringBuilder.Append(
                string.IsNullOrEmpty(finalWhereClause) ? "" : " WHERE " + finalWhereClause
            );
            sqlCommand.CommandText = stringBuilder.ToString();
        }

        /// <summary>
        /// Generates the where clause for the SQL query
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="sqlCommand"></param>
        /// <param name="definedTags"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        protected string GetWhereClause(
            QueryFilter filter,
            IDbCommand sqlCommand,
            List<DictionaryTag> definedTags
        )
        {
            string columnValueTag;
            string columnValue;
            DicomVR valueRepresentation = DicomVR.CS;
            DicomVM valueMultiplicity = DicomVM.One;
            string whereClause = string.Empty;
            string columnName = string.Empty;

            if (filter.QueryType == QueryType.And || filter.QueryType == QueryType.Or)
            {
                List<string> expressions = new List<string>();
                foreach (QueryFilter nestedFilter in filter.Expression)
                {
                    string filterWhereClause =
                        GetWhereClause(nestedFilter, sqlCommand, definedTags);
                    //TODO: In scenarios where the filter passed is Sequence
                    //QueryFilter treats it as a "MatchAll" QueryType.
                    //Hence if the where clause is null it is not added 
                    //to expressions list. To be removed after Sequence matching
                    if (!string.IsNullOrEmpty(filterWhereClause))
                    {
                        expressions.Add(filterWhereClause);
                    }
                }
                string queryTypeString =
                    (filter.QueryType == QueryType.And) ? " AND " : " OR ";
                if (expressions.Count > 0)
                {
                    whereClause = "( " + string.Join(queryTypeString, expressions) + " )";
                }

                string justJoin = string.Join(queryTypeString, expressions.ToArray());
                if (!string.IsNullOrEmpty(justJoin))
                {
                    whereClause = "( " + justJoin + " )";
                }
            }
            else
            {
                //for nested filters query type wont be available.
                if (definedTags.Contains(filter.Tag))
                {
                    columnName = GetColumnName(filter.Tag);
                    valueRepresentation = filter.Tag.ValueRepresentation;
                    valueMultiplicity = filter.Tag.ValueMultiplicity;
                }
                else
                {
                    if (filter.Tag != null)
                    {
                        //only queryable tags are allowed in query filters. otherwise Throw ex.
                        throw new ArgumentException(
                            "Invalid Queryable tag in QueryFilter: " + filter.Tag
                        );
                    }
                }
            }

            //for regular query build query string.
            switch (filter.QueryType)
            {
                case QueryType.Exists:
                    break;
                case QueryType.MatchAll:
                    break;
                case QueryType.MatchAny:
                    List<string> conditions = new List<string>();
                    foreach (string columnValueExpected in filter.Value)
                    {
                        columnValueTag = columnName + indexer++;
                        if (valueMultiplicity == DicomVM.One)
                        {
                            sqlCommand.AddParameterWithValue(columnValueTag, columnValueExpected);
                            conditions.Add(
                                string.Format(
                                CultureInfo.InvariantCulture,
                                "[{0}] = @{1}",
                                columnName,
                                columnValueTag
                                )
                                );
                        }
                        else
                        {
                            sqlCommand.AddParameterWithValue(columnValueTag,
                                "%" + columnValueExpected + "%");
                            conditions.Add(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "[{0}] LIKE @{1}",
                                    columnName,
                                    columnValueTag
                                )
                            );
                        }
                    }
                    string stringJoin = string.Join(" OR ", conditions.ToArray());
                    if (!string.IsNullOrEmpty(stringJoin))
                    {
                        whereClause = "( " + stringJoin + " )";
                    }
                    break;
                case QueryType.MatchExactDateTime:
                    columnValueTag = columnName + indexer++;
                    if (databaseType == DatabaseType.SqlServer)
                    {
                        whereClause =
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "[{0}] = CAST(@{1} AS DATETIME2)",
                                columnName,
                                columnValueTag
                            );
                    }
                    else
                    {
                        whereClause =
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "[{0}] = @{1}",
                                columnName,
                                columnValueTag
                            );
                    }
                    if (filter.LowerBound != null)
                    {
                        var filterValue = FilterMatcher.GetDateTimeAsString(filter.LowerBound.Value, filter.Tag);
                        if (databaseType == DatabaseType.Sqlite && !string.IsNullOrEmpty(filterValue))
                        {
                            string[] dateArray = new string[3];
                            dateArray[0] = filterValue.Substring(0, 4);
                            dateArray[1] = filterValue.Substring(4, 2);
                            dateArray[2] = filterValue.Substring(6, 2);
                            filterValue = string.Join("/", dateArray);
                        }
                        sqlCommand.AddParameterWithValue(
                            columnValueTag,
                            filterValue
                            );
                    }
                    break;
                case QueryType.MatchExactString:
                    columnValueTag = columnName + indexer++;
                    if (valueRepresentation == DicomVR.PN)
                    {
                        filter.Value[0] =
                            PatientNameUtility.RemoveTrailingSuffixesFromAllGroups(filter.Value[0]);
                    }
                    if (valueMultiplicity == DicomVM.One)
                    {
                        sqlCommand.AddParameterWithValue(columnValueTag, filter.Value[0]);
                        whereClause =
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "[{0}] = @{1}",
                                columnName,
                                columnValueTag
                            );
                    }
                    else
                    {
                        sqlCommand.AddParameterWithValue(
                            columnValueTag,
                            "%" + filter.Value[0] + "%"
                        );
                        whereClause =
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "[{0}] LIKE @{1}",
                                columnName,
                                columnValueTag
                            );
                    }
                    break;
                case QueryType.MatchGreaterThan:
                    if (
                        (valueRepresentation == DicomVR.DA ||
                        valueRepresentation == DicomVR.DT ||
                        valueRepresentation == DicomVR.TM)
                    )
                    {
                        whereClause =
                            FilterRangeForDateTime(
                                sqlCommand,
                                columnName,
                                filter.LowerBound,
                                filter.UpperBound,
                                filter.Tag
                            );
                    }
                    break;
                case QueryType.MatchLessThan:
                    if (
                        (valueRepresentation == DicomVR.DA ||
                        valueRepresentation == DicomVR.DT ||
                        valueRepresentation == DicomVR.TM)
                    )
                    {
                        whereClause =
                            FilterRangeForDateTime(
                                sqlCommand,
                                columnName,
                                filter.LowerBound,
                                filter.UpperBound,
                                filter.Tag
                            );
                    }
                    break;
                case QueryType.MatchRange:
                    if (
                        (valueRepresentation == DicomVR.DA ||
                        valueRepresentation == DicomVR.DT ||
                        valueRepresentation == DicomVR.TM)
                    )
                    {
                        whereClause =
                            FilterRangeForDateTime(
                                sqlCommand,
                                columnName,
                                filter.LowerBound,
                                filter.UpperBound,
                                filter.Tag
                            );
                    }
                    break;
                case QueryType.MatchSequence:
                    //TODO: implement sequence matching.
                    break;
                case QueryType.MatchWildCard:
                    columnValue = filter.Value[0];
                    if (
                        //TODO: Validate filter types and data types.
                        //(columnValue.Contains("*") || columnValue.Contains("?")) &&
                        (valueRepresentation == DicomVR.PN ||
                        valueRepresentation == DicomVR.LO ||
                        valueRepresentation == DicomVR.IS ||
                        valueRepresentation == DicomVR.SH ||
                        valueRepresentation == DicomVR.US))
                    {
                        whereClause = ConvertToSqlWildCard(
                            sqlCommand,
                            columnName,
                            columnValue);
                    }
                    break;
            }
            return whereClause;
        }

        /// <summary>
        /// If the query is given for DA, DT or TM value representations, the query has to be
        /// constructed using the BETWEEN-AND operators. The formatting of the filter strings
        /// will be done such that it can be cast to DATETIME type of SQL Server 2008.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="columnName"></param>
        /// <param name="startRange"></param>
        /// <param name="endRange"></param>
        /// <param name="dictionaryTag"></param>
        /// <returns></returns>
        private string FilterRangeForDateTime(
            IDbCommand cmd,
            string columnName,
            DateTime? startRange,
            DateTime? endRange,
            DictionaryTag dictionaryTag
        )
        {
            if (startRange == null && endRange == null)
            {
                //The input given was not correct. Hence, ignore the range.
                logger.Log("Input incorrect. Start and End Ranges not specified.", Severity.Error);
                return string.Empty;
            }

            string startDate =
                FilterMatcher.GetDateTimeAsString(startRange, dictionaryTag);
            string endDate =
                FilterMatcher.GetDateTimeAsString(endRange, dictionaryTag);

            if (databaseType == DatabaseType.Sqlite)
            {
                string[] dateArray = new string[3];
                if (startRange != null)
                {
                    dateArray[0] = startDate.Substring(0, 4);
                    dateArray[1] = startDate.Substring(4, 2);
                    dateArray[2] = startDate.Substring(6, 2);
                    startDate = string.Join("/", dateArray);
                }

                if (endRange != null)
                {
                    dateArray[0] = endDate.Substring(0, 4);
                    dateArray[1] = endDate.Substring(4, 2);
                    dateArray[2] = endDate.Substring(6, 2);
                    endDate = string.Join("/", dateArray);
                }
            }

            //If the start range is not specified, we have to query for all the entries lesser
            //than the end range.
            //If the end range is not specified, we have to query for all the entries greater 
            //than the start range.
            string tagWhereClause = string.Empty;
            if (startRange == null)
            {
                string endRangeTag = columnName + indexer++;
                cmd.AddParameterWithValue(endRangeTag, endDate);
                if (databaseType == DatabaseType.SqlServer)
                {
                    tagWhereClause =
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "[{0}] <= CAST(@{1} AS DATETIME2)",
                            columnName,
                            endRangeTag
                        );
                }
                else if (databaseType == DatabaseType.Sqlite)
                {
                    tagWhereClause =
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "[{0}] <= @{1}",
                            columnName,
                            endRangeTag
                        );
                }
            }
            else if (endRange == null)
            {
                string startRangeTag = columnName + indexer++;
                cmd.AddParameterWithValue(startRangeTag, startDate);
                if (databaseType == DatabaseType.SqlServer)
                {
                    tagWhereClause =
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "[{0}] >= CAST(@{1} AS DATETIME2)",
                            columnName,
                            startRangeTag
                        );
                }
                else if (databaseType == DatabaseType.Sqlite)
                {
                    tagWhereClause =
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "[{0}] >= @{1}",
                            columnName,
                            startRangeTag
                        );
                }
            }
            else
            {
                string startRangeTag = columnName + indexer++;
                cmd.AddParameterWithValue(startRangeTag, startDate);
                string endRangeTag = columnName + indexer++;
                cmd.AddParameterWithValue(endRangeTag, endDate);
                if (databaseType == DatabaseType.SqlServer)
                {
                    tagWhereClause =
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "[{0}] BETWEEN CAST(@{1} AS DATETIME2) AND CAST(@{2} AS DATETIME2)",
                            columnName,
                            startRangeTag,
                            endRangeTag
                        );
                }
                else if (databaseType == DatabaseType.Sqlite)
                {
                    tagWhereClause =
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "[{0}] BETWEEN @{1} AND @{2}",
                            columnName,
                            startRangeTag,
                            endRangeTag
                        );
                }
            }
            return tagWhereClause;
        }

        /// <summary>
        /// If the query issued contains any wild card character, it has to be converted to
        /// the SQL equivalent wild card characters.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="columnName"></param>
        /// <param name="columnValue"></param>
        /// <returns></returns>
        private string ConvertToSqlWildCard(
            IDbCommand cmd,
            string columnName,
            string columnValue
        )
        {
            if (databaseType == DatabaseType.SqlServer)
            {
                //Replace _ with [_] for SQLServer
                columnValue = columnValue.Replace("_", "[_]");
                //Replace % with [%] for SQLServer
                columnValue = columnValue.Replace("%", "[%]");
            }
            else
            {
                //Replace _ with \_ for SQLite
                columnValue = columnValue.Replace("_", "\\_");
                //Replace % with \% for SQLite
                columnValue = columnValue.Replace("%", "\\%");
            }

            //Replace * with %
            columnValue = columnValue.Replace('*', '%');
            //Replace ? with _
            columnValue = columnValue.Replace('?', '_');
            string columnValueTag = string.Empty;
            string convertToSqlWildCard = string.Empty;
            string[] result = columnValue.Split('|');
            foreach (String colValue in result)
            {
                columnValueTag = columnName + indexer++;
                cmd.AddParameterWithValue(
                    columnValueTag,
                    colValue);

                if (!String.IsNullOrEmpty(convertToSqlWildCard))
                {
                    convertToSqlWildCard += " OR ";
                }
                if (databaseType == DatabaseType.SqlServer)
                {
                    convertToSqlWildCard += string.Format(
                        CultureInfo.InvariantCulture,
                        "[{0}] LIKE @{1}",
                        columnName,
                        columnValueTag);
                }
                else
                {
                    convertToSqlWildCard += string.Format(
                        CultureInfo.InvariantCulture,
                        "[{0}] LIKE @{1} ESCAPE '\\'",
                        columnName,
                        columnValueTag);
                }
            }
            return convertToSqlWildCard;
        }

        /// <summary>
        /// Constructs the parent key where class and 
        /// updates the SqlParameterCollection
        /// </summary>
        /// <remarks>
        /// Identifier provides the parent key only for Series Level,
        /// For study and patient level we will not have the parent key value.
        /// TODO: Check about this with AII team
        /// </remarks>
        private string GetParentKeyWhereClause(
            Level level,
            Identifier parentIdentifier,
            IDbCommand cmd
        )
        {
            switch (level)
            {
                case Level.Series:
                    if (
                        parentIdentifier != null &&
                        !string.IsNullOrEmpty(parentIdentifier.StudyInstanceUid)
                    )
                    {
                        List<string> conditions = new List<string>();
                        string[] studyUids = parentIdentifier.StudyInstanceUid.Split('\\');
                        foreach (string studyUid in studyUids)
                        {
                            string columnValueTag = "parent" + indexer++;
                            cmd.AddParameterWithValue(
                                columnValueTag,
                                studyUid);
                            conditions.Add(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "[parUid]=@{0}",
                                    columnValueTag
                                )
                            );
                        }
                        return "( " + string.Join(" OR ", conditions.ToArray()) + " )";
                    }
                    break;
                case Level.Study:
                    if (
                        parentIdentifier != null &&
                        !parentIdentifier.PatientKey.IsDummy)
                    {
                        cmd.AddParameterWithValue(
                            "@parent",
                            PatientKeyUtility.GetPatientUidFromPatientKey(
                            parentIdentifier.PatientKey)
                            );
                        return "[parUid]=@parent";
                    }
                    break;

                case Level.Patient:
                    if (
                        parentIdentifier != null &&
                        !parentIdentifier.PatientKey.IsDummy)
                    {
                        cmd.AddParameterWithValue(
                            "@parent",
                            PatientKeyUtility.GetPatientUidFromPatientKey(
                            parentIdentifier.PatientKey)
                            );
                        return "[uid]=@parent";
                    }
                    break;
            }

            return string.Empty;
        }

        private static string GetWhereClause(
        IDbCommand cmd,
        string column,
        string value
        )
        {
            cmd.AddParameterWithValue("@" + column, value);
            return "[" + column + "]=@" + column;
        }

        /// <summary>
        /// Builds the column name for the given dictionary Tag
        /// </summary>
        /// <param name="dictionaryTag">the dictionary tag</param>
        /// <returns>the column name associated with the given tag</returns>
        public static string GetColumnName(DictionaryTag dictionaryTag)
        {
            if (string.IsNullOrEmpty(dictionaryTag.ImplementerId))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "tag_" + "{0:x8}",
                    dictionaryTag.Tag);
            }
            return string.Format(
                CultureInfo.InvariantCulture,
                "tag_" + "{0}_{1:x8}",
                CleanString(dictionaryTag.ImplementerId),
                dictionaryTag.Tag);
        }

        /// <summary>
        /// Builds the Dictionary Tag for the given column name
        /// </summary>
        /// <param name="sColumnName">the column name </param>
        /// <returns>the dictionary tag assoicated with column name</returns>
        public static DictionaryTag GetDictionaryTag(string sColumnName)
        {
            //TODO: sktonger : test and review
            if (string.IsNullOrEmpty(sColumnName))
            {
                return null;
            }
            DictionaryTag tag = null;
            string sImplementorID = "";
            uint nTagID = 0;
            if (sColumnName.Contains("tag"))
            {
                if (sColumnName.Length > 12)
                {
                    string sTagValue =
                        sColumnName.Substring(sColumnName.Length - 8, 8);
                    sImplementorID =
                        sColumnName.Substring(4, sColumnName.Length - 13); // remove tag_ and _TagID
                    sImplementorID =
                        sImplementorID.Replace('_', ' ');

                    if (!string.IsNullOrEmpty(sTagValue))
                    {
                        nTagID = Convert.ToUInt32(sTagValue, 16);
                    }
                    tag = DictionaryBase.GetDictionaryTag(nTagID, sImplementorID);
                }
                else
                {
                    string[] sValues = sColumnName.Split('_');
                    if (!string.IsNullOrEmpty(sValues[1]))
                    {
                        nTagID = Convert.ToUInt32(sValues[1], 16);
                        tag = DictionaryBase.GetDictionaryTag(nTagID, sImplementorID);
                    }
                }
            }
            return tag;
        }

        /// <summary>
        /// Returns a "cleaned" string with non-alphanumeric characters
        /// replaced with the underscore character.
        /// </summary>
        /// <param name="s">the string to be "cleaned"</param>
        /// <returns>a "cleaned" string</returns>
        private static string CleanString(string s)
        {
            StringBuilder builder = new StringBuilder(s.Length);
            foreach (char ch in s)
            {
                builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            }
            return string.Intern(builder.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <param name="level"></param>
        /// <param name="parentInstanceUid"></param>
        /// <param name="instanceUid"></param>
        /// <param name="deviceId"></param>
        public static void BuildStorePathQueryCommand(
            IDbCommand sqlCommand,
            string deviceId,
            Level level,
            string instanceUid,
            string parentInstanceUid
        )
        {
            sqlCommand.Parameters.Clear();

            StringBuilder stringBuilder = new StringBuilder(5);

            //Start Query with Select
            stringBuilder.Append("SELECT ");
            stringBuilder.Append(Constants.FileStorePathColumn);
            stringBuilder.Append(" FROM ");
            stringBuilder.Append(level);
            stringBuilder.Append(" WHERE ");

            //Add Device Id Where Clause
            string deviceIdWhereClause =
                GetWhereClause(sqlCommand, Constants.DeviceIdColumn, deviceId);
            stringBuilder.Append(deviceIdWhereClause);

            //Add UID Where Clause
            stringBuilder.Append(" AND ");
            string uidWhereClause =
                GetWhereClause(sqlCommand, Constants.UidColumn, instanceUid);
            stringBuilder.Append(uidWhereClause);

            //If level is Study or Series, Add Parent UID Where Clause
            if (
                level != Level.Patient &&
                !string.IsNullOrWhiteSpace(parentInstanceUid)
            )
            {
                stringBuilder.Append(" AND ");
                string parUidWhereClause =
                    GetWhereClause(sqlCommand, Constants.ParUidColumn, parentInstanceUid);
                stringBuilder.Append(parUidWhereClause);
            }

            sqlCommand.CommandText = stringBuilder.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <param name="level"></param>
        /// <param name="parentInstanceUid"></param>
        /// <param name="instanceUid"></param>
        /// <param name="deviceId"></param>
        public static void BuildReconciliationStatusQueryCommand(
            IDbCommand sqlCommand,
            string deviceId,
            Level level,
            string instanceUid,
            string parentInstanceUid
        )
        {
            sqlCommand.Parameters.Clear();

            StringBuilder stringBuilder = new StringBuilder(5);

            //Start Query with Select
            stringBuilder.Append("SELECT reconciliationStatus FROM ");
            stringBuilder.Append(level);
            stringBuilder.Append(" WHERE ");

            //Add Device Id Where Clause
            string deviceIdWhereClause =
                GetWhereClause(sqlCommand, Constants.DeviceIdColumn, deviceId);
            stringBuilder.Append(deviceIdWhereClause);

            //Add UID Where Clause
            stringBuilder.Append(" AND ");
            string uidWhereClause =
                GetWhereClause(sqlCommand, Constants.UidColumn, instanceUid);
            stringBuilder.Append(uidWhereClause);

            //If level is Study or Series, Add Parent UID Where Clause
            if (
                level != Level.Patient &&
                !string.IsNullOrWhiteSpace(parentInstanceUid)
            )
            {
                stringBuilder.Append(" AND ");
                string parUidWhereClause =
                    GetWhereClause(sqlCommand, Constants.ParUidColumn, parentInstanceUid);
                stringBuilder.Append(parUidWhereClause);
            }

            sqlCommand.CommandText = stringBuilder.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <param name="level"></param>
        /// <param name="parentInstanceUid"></param>
        /// <param name="instanceUid"></param>
        /// <param name="deviceId"></param>
        public static void BuildStorePathQueryCommandEx(
            IDbCommand sqlCommand,
            string deviceId,
            Level level,
            string instanceUid,
            string parentInstanceUid
        )
        {
            sqlCommand.Parameters.Clear();

            StringBuilder stringBuilder = new StringBuilder(5);

            //Start Query with Select
            stringBuilder.Append("SELECT fileStorePath FROM ");
            stringBuilder.Append(level);
            stringBuilder.Append(" WHERE ");

            //Add Device Id Where Clause
            string deviceIdWhereClause =
                Constants.DeviceIdColumn + "='" +
                deviceId + "'";
            stringBuilder.Append(deviceIdWhereClause);

            //Add UID Where Clause
            stringBuilder.Append(" AND ");
            string uidWhereClause =
                Constants.UidColumn + "='" +
                instanceUid + "'";
            stringBuilder.Append(uidWhereClause);


            //If level is Study or Series, Add Parent UID Where Clause
            if (
                level != Level.Patient &&
                !string.IsNullOrWhiteSpace(parentInstanceUid)
            )
            {
                stringBuilder.Append(" AND ");
                string parUidWhereClause =
                    Constants.ParUidColumn + "='" + parentInstanceUid + "'";
                stringBuilder.Append(parUidWhereClause);
            }

            sqlCommand.CommandText = stringBuilder.ToString();
        }

        /// <summary>
        /// Builds the column name for the given dictionary Tag
        /// </summary>
        /// <param name="dictionaryTag">the dictionary tag</param>
        /// <returns>the column name associated with the given tag</returns>
        public static string GetColumnType(DictionaryTag dictionaryTag)
        {
            string colType = null;

            if (dictionaryTag.ValueMultiplicity != DicomVM.One)
            {
                return "nvarchar(512)";
            }
            if (dictionaryTag.IsPrivate)
            {
                // TODO : Sorting based on dateTime is not possible since 
                // TODO :   we are storing them as string in DB
                // TODO : The following check is temporary and is currently made to avoid 
                // TODO :   a breaking change before assessing the impact.
                // TODO : Back-Log to be planned to assess the impact on all BIUs and 
                // TODO :   change to be made more generic 
                if (dictionaryTag == PhilipsDictionary.PiimDicomStoreDateTime)
                {
                    return "datetime";
                }
                return "nvarchar(1024)";
            }
            if (dictionaryTag.ValueRepresentation == DicomVR.AE)
            {
                colType = "nvarchar(16)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.AS)
            {
                colType = "nvarchar(20)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.CS)
            {
                colType = "nvarchar(16)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.DA)
            {
                colType = "nvarchar(32)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.DS)
            {
                colType = "nvarchar(100)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.FL)
            {
                colType = "nvarchar(100)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.IS)
            {
                colType = "nvarchar(20)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.LO)
            {
                colType = "nvarchar(64)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.LT)
            {
                colType = "nvarchar(1024)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.PN)
            {
                colType = "nvarchar(974)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.SH)
            {
                colType = "nvarchar(16)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.SL)
            {
                colType = "nvarchar(20)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.SS)
            {
                colType = "nvarchar(20)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.ST)
            {
                colType = "nvarchar(16)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.TM)
            {
                colType = "nvarchar(32)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.UI)
            {
                colType = "nvarchar(128)";
            }
            else if (dictionaryTag.ValueRepresentation == DicomVR.US)
            {
                colType = "nvarchar(16)";
            }
            else
            {
                colType = "nvarchar(1024)";
            }
            return colType;
        }

        /// <summary>
        /// Checks whether the Tags given in the SortCriteria are actually a part of the Queryables.
        /// </summary>
        /// <returns></returns>
        internal bool GetValidSortCriteria(
            Level level,
            DictionaryTagsCollection sortCriteria,
            DictionaryTagsCollection validTagsForSorting)
        {
            switch (level)
            {
                case Level.Patient:
                    foreach (var sortItem in sortCriteria)
                    {
                        if (patientQueryables.Contains(sortItem))
                        {
                            validTagsForSorting.Add(sortItem);
                        }
                    }
                    break;
                case Level.Study:
                    foreach (var sortItem in sortCriteria)
                    {
                        if (studyQueryables.Contains(sortItem))
                        {
                            validTagsForSorting.Add(sortItem);
                        }
                    }
                    break;

                case Level.Series:
                    foreach (var sortItem in sortCriteria)
                    {
                        if (seriesQueryables.Contains(sortItem))
                        {
                            validTagsForSorting.Add(sortItem);
                        }
                    }
                    break;
                default: throw new InvalidOperationException();
            }
            return validTagsForSorting.Count > 0;
        }

        /// <summary>
        /// Gets query tags for a given level
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected List<DictionaryTag> GetQueryableTags(Level level)
        {
            if (level == Level.Patient)
            {
                return patientQueryables;
            }
            if (level == Level.Study)
            {
                return studyQueryables;
            }
            if (level == Level.Series)
            {
                return seriesQueryables;
            }
            throw new InvalidOperationException("Query not supported at level: " + level);
        }
    }

}
