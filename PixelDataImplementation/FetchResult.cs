using Philips.Platform.Common;
using Philips.Platform.StorageDevices.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    //
    // Summary:
    //     Interface to initialize Philips.Platform.StorageDevices.Abstractions.IFetchResult.FetchResult
    //     and access from Pixel data implementation of persistent dicom object. Fetch result
    //     can be accessed via this object.
    internal interface IFetchResult
    {
        //
        // Summary:
        //     Philips.Platform.StorageDevicesClient.FetchResult
        FetchResult FetchResult { get; set; }
    }

    //
    // Summary:
    //     Full access attributes of the given image or frame, in the file system bulk data
    //     reference is also available in full access.
    //
    // Remarks:
    //     The information can be provided either as the path to a DICOM composite from
    //     which both full access and pixel data can be read from, or as a full access object
    //     itself, along with the reference to bulk data. The CompositePath attribute will
    //     be null in the later case.
    [Serializable]
    public class FetchResult
    {
        //
        // Summary:
        //     Gets the Philips.Platform.StorageDevicesClient.FetchResult.PixelDataType
        public PixelDataType PixelDataType { get; internal set; }

        //
        // Summary:
        //     Gets the Philips.Platform.Common.DicomObject
        public DicomObject DicomObjectHeader { get; internal set; }

        //
        // Summary:
        //     Gets the DCM file path
        public string CompositePath { get; internal set; }

        //
        // Summary:
        //     Gets the Philips.Platform.StorageDevicesClient.FetchResult.StorageKey
        public StorageKey StorageKey { get; internal set; }

        //
        // Summary:
        //     Gets the sop class uid.
        //
        // Value:
        //     The sop class uid.
        public string SopClassUid { get; private set; }

        //
        // Summary:
        //     Gets the Philips.Platform.StorageDevicesClient.FetchResult.InMemoryPixelInformation
        //
        // Value:
        //     The in-memory pixel information.
        public InMemoryPixelInformation InMemoryPixelInformation { get; internal set; }

        //
        // Summary:
        //     Initializes FetchResult class instance with specified dcm file.
        //
        // Parameters:
        //   pixelDataType:
        //     The Pixel Data type
        //
        //   dicomFile:
        //     The DCM file.
        //
        //   storageKey:
        //     The storage key.
        public FetchResult(PixelDataType pixelDataType, string dicomFile, StorageKey storageKey)
        {
            PixelDataType = pixelDataType;
            CompositePath = dicomFile;
            StorageKey = storageKey;
        }

        //
        // Summary:
        //     Initializes FetchResult class instance with specified dcm file.
        //
        // Parameters:
        //   pixelDataType:
        //     The Pixel Data type
        //
        //   dicomFile:
        //     The DCM file.
        //
        //   dicomObjectHeader:
        //     The dicom object header.
        //
        //   storageKey:
        //     The storage key.
        public FetchResult(PixelDataType pixelDataType, string dicomFile, DicomObject dicomObjectHeader, StorageKey storageKey)
        {
            PixelDataType = pixelDataType;
            CompositePath = dicomFile;
            StorageKey = storageKey;
            DicomObjectHeader = dicomObjectHeader;
        }

        //
        // Summary:
        //     Initializes FetchResult class instance with specified dcm file.
        //
        // Parameters:
        //   pixelDataType:
        //     The Pixel Data type
        //
        //   dicomFile:
        //     The DCM file.
        //
        //   dicomObjectHeader:
        //     The dicom object header.
        //
        //   storageKey:
        //     The storage key.
        //
        //   sopClassUid:
        //     The sopClassUid.
        public FetchResult(PixelDataType pixelDataType, string dicomFile, DicomObject dicomObjectHeader, StorageKey storageKey, string sopClassUid)
        {
            PixelDataType = pixelDataType;
            CompositePath = dicomFile;
            StorageKey = storageKey;
            DicomObjectHeader = dicomObjectHeader;
            SopClassUid = sopClassUid;
        }

        //
        // Summary:
        //     Initializes FetchResult class instance with specified full access object and
        //     pixel data reference.
        //
        // Parameters:
        //   pixelDataType:
        //     The Pixel Data type
        //
        //   dicomObjectHeader:
        //     The full access obj.
        //
        //   storageKey:
        //     The storage key.
        public FetchResult(PixelDataType pixelDataType, DicomObject dicomObjectHeader, StorageKey storageKey)
        {
            PixelDataType = pixelDataType;
            StorageKey = storageKey;
            DicomObjectHeader = dicomObjectHeader;
        }

        //
        // Summary:
        //     Checks and returns the bulk Reference of the given tag
        //
        // Parameters:
        //   dictionaryTag:
        //     the dictionary tag
        //
        //   bulkDataReference:
        //     output, bulk reference for the given tag
        //
        // Returns:
        //     True if Bulk reference is found, false otherwise
        public bool TryGetBulkReference(DictionaryTag dictionaryTag, out BulkDataReference bulkDataReference)
        {
            bulkDataReference = GetBulkDataReference(dictionaryTag);
            return bulkDataReference != null;
        }

        //
        // Summary:
        //     Gets the Bulk data reference for the given tag if available in header
        //
        // Parameters:
        //   dictionaryTag:
        //     The Dicom dictionary tag for which reference is required
        //
        // Returns:
        //     Bulk reference
        private BulkDataReference GetBulkDataReference(DictionaryTag dictionaryTag)
        {
            BulkDataReference result = null;
            if (DicomObjectHeader != null && DicomObjectHeader.HasTag(dictionaryTag))
            {
                DicomVR tagVR = DicomObjectHeader.GetTagVR(dictionaryTag);
                DictionaryTag tag = new DictionaryTag(dictionaryTag.Tag, tagVR, dictionaryTag.ValueMultiplicity, dictionaryTag.Name, dictionaryTag.ImplementerId);
                result = DicomObjectHeader.GetBulkDataReference(tag);
            }

            return result;
        }
    }
    }
