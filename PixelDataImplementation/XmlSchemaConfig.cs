using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;
using Philips.Platform.CommonUtilities.Configuration;
using Philips.Platform.StorageDevicesClient;
using System.Collections.Generic;

namespace PixelDataImplementation
{
    /// <summary>
    /// Reads and loads in memory patientDataBaseSchema file
    /// </summary>
    internal class XmlSchemaConfig
    {

        private readonly Dictionary<Level, List<DictionaryTag>> queryables =
            new Dictionary<Level, List<DictionaryTag>>();

        private readonly Dictionary<Level, List<DictionaryTag>> fastAccess =
            new Dictionary<Level, List<DictionaryTag>>();

        private readonly Dictionary<Level, List<DictionaryTag>> indexQueryable =
            new Dictionary<Level, List<DictionaryTag>>();

        private readonly Dictionary<Level, List<DictionaryTag>> skipUpdateAttributes =
            new Dictionary<Level, List<DictionaryTag>>();

        /// <summary>
        /// Get Queryable attributes
        /// </summary>
        public Dictionary<Level, List<DictionaryTag>> Queryables
        {
            get { return queryables; }
        }

        /// <summary>
        /// Get Fast access attributes
        /// </summary>
        public Dictionary<Level, List<DictionaryTag>> FastAccess
        {
            get { return fastAccess; }
        }

        /// <summary>
        /// Get Index queryable attributes
        /// </summary>
        public Dictionary<Level, List<DictionaryTag>> IndexQueryables
        {
            get { return indexQueryable; }
        }

        /// <summary>
        /// Get exception state update attributes
        /// </summary>
        public Dictionary<Level, List<DictionaryTag>> SkipUpdateAttributes
        {
            get { return skipUpdateAttributes; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configurationDictionary">Configuration as read from Config 2.0</param>
        public XmlSchemaConfig(IDictionary<string, object> configurationDictionary)
            : this(configurationDictionary, false)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        internal XmlSchemaConfig(IDictionary<string, object> elements, bool parsePrivate)
        {
            Parse(elements);
            if (parsePrivate)
            {
                // Ignore : Right now private parameters are expected.
            }
        }

        private void Parse(IDictionary<string, object> configurationDictionary)
        {
            var configAccess = new ConfigAccess(configurationDictionary);
            //Parse Queryable attributes section
            ParseDictionaryTags(configAccess, Constants.QueryableAttrs);
            //Parse Fastaccess attributes section
            ParseDictionaryTags(configAccess, Constants.FastAccessAttrs);
            //Parse Index queryable attributes section
            ParseDictionaryTags(configAccess, Constants.IndexAttrs);
            //Parse SkipStateAttrs section
            ParseDictionaryTags(
                configAccess, Constants.SkipStateAttrs, false);
        }

        private void ParseDictionaryTags(
            ConfigAccess configAccess, string sectionName, bool required = true)
        {
            var configSection = configAccess.GetDictionary(sectionName, required);
            switch (sectionName)
            {
                case Constants.QueryableAttrs:
                    ParseQueryableAttributes(configSection);
                    break;
                case Constants.FastAccessAttrs:
                    ParseFastAccessAttributes(configSection);
                    break;
                case Constants.IndexAttrs:
                    ParseIndexAttributes(configSection);
                    break;
                case Constants.SkipStateAttrs:
                    if (configSection != null)
                    {
                        ParseSkipStateAttributes(configSection);
                    }
                    break;
                default:
                    break;
            }
        }

        private void ParseSkipStateAttributes(ConfigAccess configSection)
        {
            skipUpdateAttributes[Level.Patient] =
                GetTags(configSection.GetStringSet(Constants.Patient, false));
            skipUpdateAttributes[Level.Study] =
                GetTags(configSection.GetStringSet(Constants.Study, false));
            skipUpdateAttributes[Level.Series] =
                GetTags(configSection.GetStringSet(Constants.Series, false));
            skipUpdateAttributes[Level.Image] =
                GetTags(configSection.GetStringSet(Constants.Image, false));
        }

        private void ParseIndexAttributes(ConfigAccess configSection)
        {
            indexQueryable[Level.Patient] =
                GetTags(configSection.GetStringSet(Constants.Patient, false));
            indexQueryable[Level.Study] =
                GetTags(configSection.GetStringSet(Constants.Study, false));
            indexQueryable[Level.Series] =
                GetTags(configSection.GetStringSet(Constants.Series, false));
            indexQueryable[Level.Image] =
                GetTags(configSection.GetStringSet(Constants.Image, false));
            indexQueryable[Level.Report] =
                GetTags(configSection.GetStringSet(Constants.Report, false));
            indexQueryable[Level.Status] =
                GetTags(configSection.GetStringSet(Constants.Status, false));
        }

        private void ParseFastAccessAttributes(ConfigAccess configSection)
        {
            fastAccess[Level.Patient] =
                GetTags(configSection.GetStringSet(Constants.Patient, false));
            fastAccess[Level.Study] =
                GetTags(configSection.GetStringSet(Constants.Study, false));
            fastAccess[Level.Series] =
                GetTags(configSection.GetStringSet(Constants.Series, false));
            fastAccess[Level.Image] =
                GetTags(configSection.GetStringSet(Constants.Image, false));
            fastAccess[Level.Report] =
                GetTags(configSection.GetStringSet(Constants.Report, false));
            fastAccess[Level.Status] =
                GetTags(configSection.GetStringSet(Constants.Status, false));
        }

        private void ParseQueryableAttributes(ConfigAccess configSection)
        {
            queryables[Level.Patient] =
                GetTags(configSection.GetStringSet(Constants.Patient, false));
            queryables[Level.Study] =
                GetTags(configSection.GetStringSet(Constants.Study, false));
            queryables[Level.Series] =
                GetTags(configSection.GetStringSet(Constants.Series, false));
            queryables[Level.Image] =
                GetTags(configSection.GetStringSet(Constants.Image, false));
            queryables[Level.Report] =
                GetTags(configSection.GetStringSet(Constants.Report, false));
            queryables[Level.Status] =
                GetTags(configSection.GetStringSet(Constants.Status, false));
            queryables[Level.Mpps] =
                GetTags(configSection.GetStringSet(Constants.Mpps, false));
        }

        private static List<DictionaryTag> GetTags(IEnumerable<string> tags)
        {
            var dictionaryTags = new List<DictionaryTag>();
            if (tags == null)
            {
                return dictionaryTags;
            }
            foreach (var tag in tags)
            {
                var dictionaryTag = DictionaryBase.GetDictionaryTag(tag);
                if (dictionaryTag != null)
                {
                    if (!dictionaryTags.Contains(dictionaryTag))
                    {
                        dictionaryTags.Add(dictionaryTag);
                    }
                }
            }
            return dictionaryTags;
        }
    }
}