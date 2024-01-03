using Philips.Platform.Common;
using Philips.Platform.CommonUtilities.Configuration;
using Philips.Platform.CommonUtilities.Logging;
using Severity = Philips.Platform.CommonUtilities.Logging.Severity;

using Philips.Platform.StorageDevicesClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// Provides mechanism to read PatientDatabase.xml
    /// </summary>
    internal class XmlSchemaReader : ISchemaReader
    {
        private readonly string configurationFile;
        private XmlSchemaConfig xmlSchemaConfig;
        private static readonly DevelopmentLogger developmentLogger = new DevelopmentLogger(
            "XmlSchemaReader", typeof(XmlSchemaReader).Namespace);

        /// <summary>
        /// constructor
        /// </summary>
        public XmlSchemaReader(string configFileName)
        {
            configurationFile = configFileName;
        }

        /// <summary>
        /// Property injection: Inject xmlSchemaConfig.
        /// </summary>
        public XmlSchemaConfig XmlSchemaConfig
        {
            get { return xmlSchemaConfig; }
            internal set { xmlSchemaConfig = value; }
        }
        /// <summary>
        /// Load configuration
        /// </summary>
        public void Load()
        {
            xmlSchemaConfig = ConfigurationToolkit.SystemConfiguration.
                CreateStronglyTypedConfigObject(
                typeof(XmlSchemaConfig), configurationFile) as XmlSchemaConfig;
            var message = string.Format(
                CultureInfo.InvariantCulture,
                "Database schema initialized"
                );
            developmentLogger.Log(message, Severity.Info);
        }
        /// <summary>
        /// Get queryable tags
        /// </summary>
        public List<DictionaryTag> GetQueryableTags(Level level)
        {
            var queryables = xmlSchemaConfig.Queryables;
            List<DictionaryTag> dicTags;
            queryables.TryGetValue(level, out dicTags);
            return dicTags ?? new List<DictionaryTag>();
        }
        /// <summary>
        /// Get fast access tags
        /// </summary>
        public List<DictionaryTag> GetFastAccessTags(Level level)
        {
            var fastAccess = xmlSchemaConfig.FastAccess;
            List<DictionaryTag> dicTags;
            fastAccess.TryGetValue(level, out dicTags);
            return dicTags ?? new List<DictionaryTag>();
        }
        /// <summary>
        /// Get index queryable 
        /// </summary>
        public List<DictionaryTag> GetIndexQueryable(Level level)
        {
            var indexQueryable = xmlSchemaConfig.IndexQueryables;
            List<DictionaryTag> dicTags;
            indexQueryable.TryGetValue(level, out dicTags);
            return dicTags ?? new List<DictionaryTag>();
        }
        /// <summary>
        /// Get skipped exception state attributes  
        /// </summary>
        public List<DictionaryTag> GetTagsToSkipModification(Level level)
        {
            var skipStateUpdateAttrs = xmlSchemaConfig.SkipUpdateAttributes;
            List<DictionaryTag> dicTags;
            skipStateUpdateAttrs.TryGetValue(level, out dicTags);
            return dicTags ?? new List<DictionaryTag>();
        }
        /// <summary>
        /// Get dicomdir tags
        /// </summary>
        public List<DictionaryTag> GetDicomDirTags(Level level)
        {
            return new List<DictionaryTag>();
        }

        /// <summary>
        /// Get sop class Uid
        /// </summary>
        public List<string> GetSopClassUids()
        {
            return new List<string>();
        }
        /// <summary>
        /// Get SIDS attribute
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetSidsAttributes()
        {
            return new Dictionary<string, string>();
        }
        /// <summary>
        /// Get database service config
        /// </summary>
        public Dictionary<string, string> GetDatabaseServiceConfig()
        {
            return new Dictionary<string, string>();
        }
        /// <summary>
        /// Get cleanup config
        /// </summary>
        public Dictionary<string, string> GetCleanupConfig()
        {
            return new Dictionary<string, string>();
        }
    }
}
