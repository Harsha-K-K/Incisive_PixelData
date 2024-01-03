using Philips.Platform.Common;
using Philips.Platform.StorageDevicesClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// Provides information about study and series level dicom tags.
    /// </summary>
    internal class StudySeriesAttributeProvider
    {
        private static readonly object lockObject = new object();
        private static StudySeriesAttributeProvider instance;
        private static string patientDatabaseSchemaFile = "PatientDatabaseSchema.xml";
        /// <summary>
        /// Provides a mechanism to read database schema and retrieve fast access tags at given level i.e. study,series or image.
        /// </summary>
        private readonly ISchemaReader configurationReader;

        //TODO:: Make all these properties as internal once PatientKeyImplementation is
        //moved to StorageDevices.Common project.Then we can make internals of this project
        //visible to StorageDevices test project.
        /// <summary>
        /// List of dicom tags to be stored at study level.
        /// </summary>
        public IList<DictionaryTag> StudyAttributes { get; private set; }

        /// <summary>
        /// List of dicom tags to be stored at series level.
        /// </summary>
        public IList<DictionaryTag> SeriesAttributes { get; private set; }

        /// <summary>
        /// List of dicom tags which can be used when querying at Study level
        /// </summary>
        public IList<DictionaryTag> StudyQueryAttributes { get; private set; }

        /// <summary>
        /// List of dicom tags which can be used when querying at Series level
        /// </summary>
        public IList<DictionaryTag> SeriesQueryAttributes { get; private set; }
        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static StudySeriesAttributeProvider Instance
        {
            get
            {
                //TICS -COV_CS_LOCK_EVASION
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new StudySeriesAttributeProvider();
                        }
                    }
                }
                //TICS +COV_CS_LOCK_EVASION
                return instance;
            }
        }

        private StudySeriesAttributeProvider()
        {
            configurationReader = ReadDataBaseConfig();
            InitializeStudyAndSeriesAttributes();
        }

        private ISchemaReader ReadDataBaseConfig()
        {
            ISchemaReader xmlSchemaReader =
                new XmlSchemaReader(patientDatabaseSchemaFile);
            xmlSchemaReader.Load();
            return xmlSchemaReader;
        }

        /// <summary>
        /// Parameterized constructor to initialize ISchemaReader from test cases.
        /// </summary>
        /// <param name="schemaReader">ISchemaReader</param>
        internal StudySeriesAttributeProvider(ISchemaReader schemaReader)
        {
            configurationReader = schemaReader;
            //The order of initialization should not be changed here.
            InitializeStudyAndSeriesAttributes();
        }

        private void InitializeStudyAndSeriesAttributes()
        {
            StudyAttributes = configurationReader.GetFastAccessTags(Level.Study);
            SeriesAttributes = new List<DictionaryTag>();
            var seriesFastAccessTags =
                configurationReader.GetFastAccessTags(Level.Series);
            foreach (var tag in seriesFastAccessTags)
            {
                if (!StudyAttributes.Contains(tag))
                {
                    SeriesAttributes.Add(tag);
                }
            }

            StudyQueryAttributes = configurationReader.GetQueryableTags(Level.Study);
            SeriesQueryAttributes = new List<DictionaryTag>();
            var seriesQueryTags = configurationReader.GetQueryableTags(Level.Series);
            foreach (var tag in seriesQueryTags)
            {
                if (!StudyQueryAttributes.Contains(tag))
                {
                    SeriesQueryAttributes.Add(tag);
                }
            }
        }
    }
}
