using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Philips.Platform.Adapters;
using Philips.Platform.Adapters.FastInMemory;
using Philips.Platform.Adapters.MemoryManager;
using Philips.Platform.Adapters.Services.LoadAdapter;
using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.ApplicationIntegration.Tracing;
using Philips.Platform.Common;
using Philips.Platform.Common.DataAccess;
using Philips.Platform.Connectivity;
using Philips.Platform.Dicom;
using Philips.Platform.StorageDevices;
using Philips.Platform.StorageDevices.Abstractions;
using Philips.Platform.StorageDevices.FastInMemory;
using Philips.Platform.StorageDevices.Web;
using Philips.Platform.StorageDevicesClient;
using DicomDictionary = Philips.Platform.ApplicationIntegration.DataAccess.DicomDictionary;
using DicomObject = Philips.Platform.Common.DicomObject;
using PhilipsDictionary = Philips.Platform.Dicom.PhilipsDictionary;

namespace PixelDataImpl
{
    /// <summary>
    /// Implements the <see cref="PixelData"/> interface for DB 2.0
    /// </summary>
#pragma warning disable 0618, 0612//Disabling obsolete warnings
    public class PixelDataImplCopy : PixelData, IFetchResult
    {
        //TODO: IMemoryMappedUnit and memoryManagerClient throws variety of exceptions. These 
        //exceptions needs to be caught in IPixelData implementations and should be re thrown as 
        //appropriate exceptions that are defined for IPixelData. exception behavior is yet to
        //defined for IPixelData.


        #region Private/Internal Fields
        /// <summary>
        /// Flag indicating if fast repository is enabled in current deployment.
        /// </summary>
        private readonly bool isFastRepositoryEnabled;
        private readonly string sopInstanceUid;
        private InMemoryPixelInformation fastInMemoryPixelInformation;
        /// <summary>
        /// Implements the <see cref="IFastInMemoryPixelDataHandler"/>
        /// Set has been exposed for test cases.
        /// </summary>
        internal IFastInMemoryPixelDataHandler FastInMemoryPixelDataHandler { get; set; }
        /// <summary>
        /// Performance class used to trigger process level performance tracing
        /// </summary>
        private static readonly PerformanceTracer performanceTracer =
            PerformanceTracer.CreatePerformanceTracer(typeof(PixelDataImplCopy));
        private static readonly PredefinedAction LoadPixelAction =
            new PredefinedAction("LoadPixel");

        /// <summary>
        /// 
        /// </summary>
        public IMemoryMappedUnit MemoryMappedUnit
        {
            get;
            private set;
        }

        private int bulkSize;

        private DicomObject imageDicomObject;

        private readonly object bulkDataRefSyncObject = new object();

        /// <summary>
        /// Maintains the number of references to of bulk-data.
        /// </summary>
        private int bulkDataRefCount;

        private ImagePixelDescription imagePixelDescription;

        private BulkDataReference pixelDataReference;
        /// <summary>
        /// <see cref="IFetchResult.FetchResult"/>
        /// </summary>
        public FetchResult FetchResult
        {
            get;
            set;
        }

        private readonly PixelDataType pixelDataType;

        private bool disposed;

        private static readonly DictionaryTag[] mandatoryPixelModuleAttributes =
            new[] {
                DicomDictionary.DicomBitsAllocated,
                DicomDictionary.DicomBitsStored,
                DicomDictionary.DicomHighBit,
                DicomDictionary.DicomPhotometricInterpretation,
                DicomDictionary.DicomPixelRepresentation,
                DicomDictionary.DicomRows,
                DicomDictionary.DicomColumns,
                DicomDictionary.DicomSamplesPerPixel
            };

        private static readonly HashSet<string> unConvertedPhotoMetric =
            new HashSet<string> {
                "YBR_FULL",
                "YBR_FULL_422",
                "YBR_PARTIAL_422",
                "YBR_RCT",
                "YBR_ICT"
            };

        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of PixelDataImplementation with given dcm file path
        /// </summary>
        /// <param name="fetchResult">the fetch result</param>
        //public PixelDataImplCopy(FetchResult fetchResult) : this(fetchResult, false)
        //{
        //}

        ///// <summary>
        ///// Initializes a new instance of PixelDataImplementation with given dcm file path
        ///// </summary>
        ///// <param name="fetchResult">the fetch result</param>
        ///// <param name="isFullHeaderLoad"></param>
        //public PixelDataImplCopy(FetchResult fetchResult, bool isFullHeaderLoad)
        //{
        //    FetchResult = fetchResult;
        //    pixelDataType = fetchResult.PixelDataType;
        //    imageDicomObject = FetchResult.DicomObjectHeader;

        //    if (pixelDataType == PixelDataType.None)
        //    {
        //        LogHelper.LogInfo(
        //            "Received non pixel data type" +
        //            fetchResult.CompositePath.Anonymize());
        //        return;
        //    }
        //    if (imageDicomObject != null)
        //    {
        //        isFastRepositoryEnabled = FastInMemoryUtility.IsFastRepositoryEnabled();
        //        if (isFastRepositoryEnabled)
        //        {
        //            sopInstanceUid = imageDicomObject.GetString(DicomDictionary.DicomSopInstanceUid);
        //            FastInMemoryPixelDataHandler = new FastInMemoryPixelDataHandler();
        //            return;
        //        }
        //        pixelDataReference =
        //            imageDicomObject.GetBulkDataReference(
        //                StorageDevicesUtilities.GetPixelDataTag(
        //                    imageDicomObject.GetTagVR(DicomDictionary.DicomPixelData)));
        //    }
        //    else
        //    {
        //        if (ConfigurationProvider.Instance.IsPresentationState(FetchResult.SopClassUid))
        //        {
        //            imageDicomObject =
        //                DicomSerializer.Deserialize(FetchResult.CompositePath, true, true, true);
        //        }
        //        else
        //        {
        //            LoadComposite(isFullHeaderLoad);
        //        }
        //    }
        //}

        /// <summary>
        /// Initializes a new instance of PixelDataImplementation with given dcm file path
        /// </summary>
        /// <param name="fetchResult">the fetch result</param>
        /// <param name="isFullHeaderLoad"></param>
        public PixelDataImplCopy(DicomObject dcm)
        {
            imageDicomObject = dcm;

            if (pixelDataType == PixelDataType.None)
            {
                //LogHelper.LogInfo(
                //    "Received non pixel data type" +
                //    fetchResult.CompositePath.Anonymize());
                return;
            }
            if (imageDicomObject != null)
            {
                //isFastRepositoryEnabled = FastInMemoryUtility.IsFastRepositoryEnabled();
                //if (isFastRepositoryEnabled)
                //{
                //    sopInstanceUid = imageDicomObject.GetString(DicomDictionary.DicomSopInstanceUid);
                //    FastInMemoryPixelDataHandler = new FastInMemoryPixelDataHandler();
                //    return;
                //}
                pixelDataReference =
                    imageDicomObject.GetBulkDataReference(
                        StorageDevicesUtilities.GetPixelDataTag(
                            imageDicomObject.GetTagVR(DicomDictionary.DicomPixelData)));
            }
            //else
            //{
            //    if (ConfigurationProvider.Instance.IsPresentationState(FetchResult.SopClassUid))
            //    {
            //        imageDicomObject =
            //            DicomSerializer.Deserialize(FetchResult.CompositePath, true, true, true);
            //    }
            //    else
            //    {
            //        LoadComposite(isFullHeaderLoad);
            //    }
            //}
        }


        ///// <summary>
        ///// Initializes a new instance of PixelDataImplementation with given dcm file path
        ///// </summary>
        //public PixelDataImplCopy(
        //    DicomObject imageHeader,
        //    BulkDataReference bulkDataReference
        //)
        //{
        //    pixelDataType = PixelDataType.FileOffsetLength;
        //    imageDicomObject = imageHeader;
        //    pixelDataReference = bulkDataReference;
        //}

        #endregion

        /// <summary>
        /// Calls a dispose on memory mapped unit
        /// </summary>
        ~PixelDataImplCopy()
        {
            Dispose(false);
        }

        /// <summary>
        /// For disposing managed and unmanaged resources
        /// </summary>
        /// <param name="disposing">true if called by GC</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //free managed resources
                    DisposeMemoryMappedUnit();
                    if (imageDicomObject != null)
                    {
                        imageDicomObject.Dispose();
                        imageDicomObject = null;
                    }
                }
                disposed = true;
            }
        }

        internal void CleanInternalPixelDataLoader()
        {
            lock (bulkDataRefSyncObject)
            {
                if (bulkDataRefCount == 0)
                {
                    DisposeMemoryMappedUnit();
                }
            }
        }

        private void DisposeMemoryMappedUnit()
        {
            if (MemoryMappedUnit != null)
            {
                MemoryMappedUnit.Dispose();
                MemoryMappedUnit = null;
            }
        }


        private void LoadBulkData()
        {
            TriggerLoadPixel();
            WaitForLoadPixelCompletion();
        }
        //TICS -6@201 cyclomatic complexity
        private void TriggerLoadPixel()
        {
            if (isFastRepositoryEnabled)
            {
                if (fastInMemoryPixelInformation == null && MemoryMappedUnit == null)
                {
                    fastInMemoryPixelInformation = MemoryManagerClient.RetrieveInMemoryPixelInformation(sopInstanceUid);
                }
                if (fastInMemoryPixelInformation != null) return;
            }
            InitializeMemoryMappedUnit();
        }

        private void InitializeMemoryMappedUnit()
        {
            if (MemoryMappedUnit != null && !MemoryMappedUnit.MemoryMappedUnitName.Contains("_Header"))
            {
                return;
            }
            // Check once again if it has BDR to avoid load composite
            // It might be updated with BDR after been constructed
            if (imageDicomObject != null)
            {
                pixelDataReference = imageDicomObject.GetBulkDataReference(
                    StorageDevicesUtilities.GetPixelDataTag(imageDicomObject.GetTagVR(DicomDictionary.DicomPixelData)));
            }

            //if (FetchResult.PixelDataType != PixelDataType.Wado &&
            //    FetchResult.PixelDataType != PixelDataType.Ahli &&
            //    (pixelDataReference == null || imageDicomObject == null))
            //{
            //    LoadComposite();
            //}
            //else
            //{
                var pixelInformation = GetFileInformation();

                if (pixelInformation != null)
                {
                    if (imageDicomObject != null)
                    {
                        pixelInformation.ImageConversionInformation = new ImageConversionInformation(imageDicomObject);
                    }
                    if (Philips.Platform.Adapters.DecompressionUtil.IsCompressedTransferSyntax(
                        pixelInformation.ImageConversionInformation.TransferSyntaxUid))
                    {
                        if (imageDicomObject != null)
                        {
                            pixelInformation.CompressionInformation = new CompressionInformation(imageDicomObject);
                        }
                    }
                }
                MemoryMappedUnit = MemoryManagerOpenFileWithPreLoad(pixelInformation);
            //}
        }
        //TICS +6@201 cyclomatic complexity

        /// <summary>
        /// Gets the file information.
        /// </summary>
        /// <returns>The file information.</returns>
        private PixelDataInformation GetFileInformation()
        {
            PixelDataInformation fileInformation;
            //if (pixelDataReference == null & FetchResult.PixelDataType == PixelDataType.Wado)
            //{
            //    var httpHeaderAttributes = GetHttpHeaderAttributes();
            //    return new WadoPixelDataInformation(FetchResult.CompositePath, httpHeaderAttributes);
            //}
            //if (pixelDataReference == null & FetchResult.PixelDataType == PixelDataType.Ahli)
            //{
            //    return CreateAhliPixelDataInformation();
            //}
            if (pixelDataReference == null)
            {
                return null;
            }
            //if (IsValidUrl(pixelDataReference.FileName))
            //{
            //    var httpHeaderAttributes = GetHttpHeaderAttributes();
            //    fileInformation =
            //        new WadoPixelDataInformation(pixelDataReference.FileName, httpHeaderAttributes);
            //    if (FetchResult.StorageKey.Identifier.FrameNumber != 0)
            //    {
            //        fileInformation.FrameNumber =
            //            Convert.ToInt32(FetchResult.StorageKey.Identifier.FrameNumber);
            //    }

            //}
            //else
            //{
                // Call memory manager to open the file. This may throw an exception,
                // which will be passed on to the caller.
                fileInformation =
                    new FileInformation(
                        pixelDataReference.FileName,
                        pixelDataReference.Length,
                        pixelDataReference.Offset);
            //}

            return fileInformation;
        }

        //private PixelDataInformation CreateAhliPixelDataInformation()
        //{
        //    var httpHeaderAttributes = GetHttpHeaderAttributes();
        //    var dataStoreId = FetchResult.DicomObjectHeader.GetString(PhilipsDictionary.PiimCurieDataStoreId);
        //    var imageSetId = FetchResult.DicomObjectHeader.GetString(PhilipsDictionary.PiimCurieImageSetId);
        //    var seriesInstanceUid = FetchResult.DicomObjectHeader.GetString(DicomDictionary.DicomSeriesInstanceUid);
        //    var sopUid = FetchResult.DicomObjectHeader.GetString(DicomDictionary.DicomSopInstanceUid);
        //    int frameIndex = checked((int)FetchResult.StorageKey.Identifier.FrameNumber);
        //    var frameId = FetchResult.DicomObjectHeader.GetStringArray(PhilipsDictionary.PiimCurieFrameIds)
        //        .ElementAt(frameIndex);
        //    return new AhliPixelDataInformation(
        //        FetchResult.CompositePath,
        //        httpHeaderAttributes,
        //        dataStoreId,
        //        imageSetId,
        //        seriesInstanceUid,
        //        sopUid,
        //        frameId
        //        );
        //}

        //private Dictionary<string, string> GetHttpHeaderAttributes()
        //{
        //    var dicomWebDeviceId = FetchResult.StorageKey.SourceDevice;
        //    var device = (DicomWebDevice)DicomWebDeviceManager.Instance.GetDevice(dicomWebDeviceId);
        //    if (device == null)
        //    {
        //        return new Dictionary<string, string>();
        //    }
        //    return device.GetHttpHeaderAttributes();
        //}
        /// <summary>
        /// Checks whether given string is a valid url or not.
        /// </summary>
        /// <param name="input">String to be checked for url.</param>
        /// <returns>True if the given string is valid url otherwise false.</returns>
        private static bool IsValidUrl(string input)
        {
            Uri outUri;
            if (
                Uri.TryCreate(input, UriKind.Absolute, out outUri) &&
                (outUri.Scheme == Uri.UriSchemeHttp ||
                outUri.Scheme == Uri.UriSchemeHttps))
            {
                return true;
            }
            return false;
        }

        //private void LoadComposite(bool isLoadFullHeader = false)
        //{
        //    if (
        //        pixelDataType != PixelDataType.DicomFile ||
        //        string.IsNullOrEmpty(FetchResult.CompositePath)
        //    )
        //    {
        //        return;
        //    }

        //    // Call memory manager to open the file. This may throw an exception,
        //    // which will be passed on to the caller.
        //    FileInformation fileInformation =
        //        new FileInformation(FetchResult.CompositePath, 0, 0)
        //        {
        //            ReadAttributesFromFile = true
        //        };

        //    // If frameNumber is set to a valid frame number(uint other than 0), then set
        //    // isMultiFrameImage to true.
        //    if (FetchResult.StorageKey.Identifier.FrameNumber != 0)
        //    {
        //        fileInformation.IsMultiframeImage = true;
        //        fileInformation.FrameNumber =
        //            Convert.ToInt32(FetchResult.StorageKey.Identifier.FrameNumber);
        //    }
        //    MemoryMappedUnit = MemoryManagerOpenFileWithPreLoad(fileInformation, isLoadFullHeader);
        //}

        private void LockPixel()
        {
            //if (isFastRepositoryEnabled)
            //{
            //    if (pixels == IntPtr.Zero && fastInMemoryPixelInformation == null)
            //    {
            //        fastInMemoryPixelInformation = MemoryManagerClient.RetrieveInMemoryPixelInformation(sopInstanceUid);
            //    }
            //    if (fastInMemoryPixelInformation != null)
            //    {
            //        if (pixels == IntPtr.Zero)
            //        {
            //            TryLoadingPixelFromFastInMemory();
            //        }
            //        if (pixels != IntPtr.Zero)
            //        {
            //            bulkDataRefCount = Interlocked.Increment(ref bulkDataRefCount);
            //            LogHelper.DevelopmentDebugLog("Pixels loaded from Fast In-memory repository device.");
            //            return;
            //        }
            //        //If pixels could not be loaded from fastInMemory fall back and let it get loaded from MMU.
            //        InitializeMemoryMappedUnit();
            //    }
            //}
            if (MemoryMappedUnit != null)
            {
                pixels = MemoryMappedUnit.Lock(out bulkSize);
                bulkDataRefCount = Interlocked.Increment(ref bulkDataRefCount);
            }
        }

        //private void TryLoadingPixelFromFastInMemory()
        //{
        //    try
        //    {
        //        pixels = FastInMemoryPixelDataHandler.LoadPixel(fastInMemoryPixelInformation);
        //    }
        //    catch (Exception ex)
        //    {
        //        //This can occur during a race condition between LoadPixel, Delete from fast repository and LockPixel.
        //        LogHelper.LogWarning(
        //            "Unable to load data from fast in memory pixel. Hence, falling back to file system.", ex);
        //        fastInMemoryPixelInformation = null;
        //    }
        //}

        private static IMemoryMappedUnit MemoryManagerOpenFileWithPreLoad(
            PixelDataInformation pixelInformation,
            bool isLoadFullHeader = false
            )
        {
            try
            {
                return MemoryManagerClient.OpenFile(
                    pixelInformation,
                    isLoadFullHeader,
                    true);
            }
            catch (FileNotFoundException ex)
            {
                throw new DataCorruptException(ex.Message, ex.InnerException);
            }

        }

        /// <summary>
        /// Gets the <see cref="DicomObject"/> for this pixel data
        /// </summary>
        public DicomObject DicomObject
        {
            get
            {
                if (
                    imageDicomObject == null &&
                    pixelDataType == PixelDataType.DicomFile &&
                    MemoryMappedUnit != null
                )
                {
                    //Based on AII Load API Definition, header is always unconverted
                    //Hence getting unconverted header from MMU
                    imageDicomObject = MemoryMappedUnit.UnconvertedDicomObject.ShallowCopy();
                    imageDicomObject = DicomObjectUtility.DeepClone(imageDicomObject);
                }
                return imageDicomObject;
            }
        }

        /// <summary>
        /// <see cref="PixelData.LoadAndLockPixels()"/>
        /// </summary>
        public override void LoadAndLockPixels()
        {
            var loadPerformanceToken =
                performanceTracer.TraceBeginVerbose(
                LoadPixelAction,
                "LoadPixelAndLock"
            );
            lock (bulkDataRefSyncObject)
            {
                TriggerLoadPixel();
                LockPixel();
                performanceTracer.TraceEnd(loadPerformanceToken);
            }
        }

        /// <summary>
        /// <see cref="PixelData.LoadPixels()"/>
        /// </summary>
        public override void LoadPixels()
        {
            lock (bulkDataRefSyncObject)
            {
                LoadBulkData();
            }
        }

        internal void LoadPixelsAsync()
        {
            lock (bulkDataRefSyncObject)
            {
                TriggerLoadPixel();
            }
        }
        internal void WaitForLoadPixelCompletion()
        {
            lock (bulkDataRefSyncObject)
            {
                if (MemoryMappedUnit != null)
                {
                    MemoryMappedUnit.LoadPixels();
                }
            }
        }

        /// <summary>
        /// Retaining old Lock() method
        /// </summary>
        public override void Lock()
        {
            var loadPerformanceToken =
                performanceTracer.TraceBeginVerbose(
                LoadPixelAction,
                "Lock"
            );

            lock (bulkDataRefSyncObject)
            {
                LockPixel();
                performanceTracer.TraceEnd(loadPerformanceToken);
            }
        }

        /// <summary>
        /// <see cref="PixelData.Size"/>
        /// </summary>
        public override int Size
        {
            get
            {
                if (isFastRepositoryEnabled && fastInMemoryPixelInformation != null)
                {
                    bulkSize = (int)fastInMemoryPixelInformation.PixelLength;
                }
                if (HasPixels && bulkSize == 0)
                {
                    //TODO: Bulk Size calculation for Non-RGB
                    bulkSize =
                        Description.Rows *
                        Description.Columns *
                        ((Description.BitsAllocated + 7) / 8) *
                        Description.SamplesPerPixel;
                }
                return bulkSize;
            }
        }

        /// <summary>
        /// <see cref="PixelData.Unlock()"/>
        /// </summary>
        public override int Unlock()
        {
            lock (bulkDataRefSyncObject)
            {
                if (bulkDataRefCount != 0)
                {
                    if (MemoryMappedUnit != null)
                    {
                        MemoryMappedUnit.Unlock();
                    }
                    bulkDataRefCount = Interlocked.Decrement(ref bulkDataRefCount);
                    if (bulkDataRefCount == 0)
                    {
                        pixels = IntPtr.Zero;
                        ReleaseFastInMemoryPixelResources();
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        "One excess Unlock has been called than actual" +
                        " Lock() or LoadAndLockPixesl() calls");
                }
                return bulkDataRefCount;
            }
        }

        private void ReleaseFastInMemoryPixelResources()
        {
            if (isFastRepositoryEnabled)
            {
                fastInMemoryPixelInformation = null;
                FastInMemoryPixelDataHandler?.ReleaseResources();
            }
        }

        /// <summary>
        /// <see cref="PixelData.UnlockAndClean()"/>
        /// </summary>
        [ProposedApi]
        public override void UnlockAndClean()
        {
            lock (bulkDataRefSyncObject)
            {
                if (bulkDataRefCount != 0)
                {
                    if (MemoryMappedUnit != null)
                    {
                        MemoryMappedUnit.Unlock();
                    }
                    bulkDataRefCount = Interlocked.Decrement(ref bulkDataRefCount);
                    if (bulkDataRefCount == 0)
                    {
                        pixels = IntPtr.Zero;
                        ReleaseFastInMemoryPixelResources();
                        if (!disposed)
                        {
                            MemoryMappedUnit.ReleaseResources();
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        "One excess Unlock has been called than actual" +
                        " Lock() or LoadAndLockPixesl() calls");
                }
            }
        }

        /// <summary>
        /// <see cref="PixelData.SupportsUnlockAndClean()"/>
        /// </summary>
        /// <returns>Returns true</returns>
        [ProposedApi]
        public override bool SupportsUnlockAndClean()
        {
            return true;
        }

        internal void UnlockIfLocked()
        {
            lock (bulkDataRefSyncObject)
            {
                if (bulkDataRefCount != 0)
                {
                    Unlock();
                }
            }
        }

        /// <summary>
        /// <see cref="PixelData.MarkForCleanup"/>
        /// </summary>
        /// <returns></returns>
        public override void MarkForCleanup()
        {
            lock (bulkDataRefSyncObject)
            {
                if (bulkDataRefCount == 0 && MemoryMappedUnit != null)
                {
                    MemoryMappedUnit.MarkForCleanup();
                }
            }
        }

        /// <summary>
        /// <see cref="PixelData.Description"/>
        /// </summary>
        public override ImagePixelDescription Description
        {
            get
            {
                if (isFastRepositoryEnabled && fastInMemoryPixelInformation != null)
                {
                    imagePixelDescription = FastInMemoryPixelDataHandler.GetImagePixelDescription(imageDicomObject);
                }
                if (imagePixelDescription == null)
                {
                    ImageConversionInformation imageConversionInformation;
                    lock (bulkDataRefSyncObject)
                    {
                        if (
                            MemoryMappedUnit != null &&
                            MemoryMappedUnit.ModifiedImageConversionInformation != null
                        )
                        {
                            imageConversionInformation =
                                MemoryMappedUnit.ModifiedImageConversionInformation;
                        }
                        else
                        {
                            imageConversionInformation =
                                new ImageConversionInformation(imageDicomObject);
                            var imageTranslator =
                                new ImageTranslator(
                                    imageConversionInformation, imageDicomObject, true);
                            imageConversionInformation =
                                imageTranslator.GetModifiedImageConversionInformation();
                        }
                    }
                    //In Scenarios where we are building the ImageConversion from
                    //Image Header, ensuring that image conversion is set to the
                    //uncompressed format, cause MemoryManager always provides us with
                    //uncompressed pixels.
                    //TODO: We will get this information from Memory Manager, but we
                    //need this info upfront, hence this patch.
                    //TODO: We need to revisit this.
                    if (
                        unConvertedPhotoMetric.Contains(
                        imageConversionInformation.PhotometricInterpretation)
                    )
                    {
                        imageConversionInformation.PhotometricInterpretation =
                            MemoryManagerConstants.RGB;
                    }
                    PlanarConfiguration planarConfig =
                        GetPlanarConfiguration(imageConversionInformation.PlanarConfiguration);
                    bool isImageConverted =
                        (imageConversionInformation.ImageConversionType != ImageConversions.None);
                    imagePixelDescription = ImagePixelDescriptionHelper.GetImagePixelDescription(
                        planarConfig,
                        isImageConverted,
                        imageConversionInformation);

                    StringBuilder writer = new StringBuilder();
                    PropertyInfo[] props = imagePixelDescription.GetType().GetProperties();
                    foreach (PropertyInfo prop in props)
                    {
                        object propValue = prop.GetValue(imagePixelDescription, null);
                        writer.AppendLine($"{prop.Name} : {propValue}");
                    }
                    writer.AppendLine($"Pixel Size = {Size}");
                    File.AppendAllText($@"D:\MyLogs\PixelDataOriginal\log.txt", writer.ToString());
                }
                return imagePixelDescription;
            }
        }

        private IntPtr pixels = IntPtr.Zero;

        /// <summary>
        /// <see cref="PixelData.Pixels"/>
        /// </summary>
        public override IntPtr Pixels
        {
            get { return pixels; }
        }
        /// <summary>
        /// Returns true if the PixelData has pixels, false otherwise
        /// </summary>
        internal bool HasPixels
        {
            get
            {
                if (DicomObject == null)
                {
                    return false;
                }
                foreach (DictionaryTag mandatoryTag in mandatoryPixelModuleAttributes)
                {
                    if (!DicomObject.HasTag(mandatoryTag))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Gets the planar configuration.
        /// </summary>
        /// <param name="planarConfiguration">The planar configuration.</param>
        /// <returns>PlanarConfiguration</returns>
        private static PlanarConfiguration GetPlanarConfiguration(
            ReadOnlyArray<ushort> planarConfiguration
        )
        {
            PlanarConfiguration planarConfig = PlanarConfiguration.PixelInterleaved;
            if (
                planarConfiguration.Count > 0 &&
                planarConfiguration[0] == 1
            )
            {
                planarConfig = PlanarConfiguration.PlanarInterleaved;
            }
            return planarConfig;
        }

    }
}
