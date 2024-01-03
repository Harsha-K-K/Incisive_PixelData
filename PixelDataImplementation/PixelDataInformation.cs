using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// This class holds the file information required by
    /// Memory manager to load the pixels.
    /// </summary>
    [Serializable]
    [DataContract(Name = "PixelDataInformation")]
    [KnownType(typeof(WadoPixelDataInformation))]
    [KnownType(typeof(AhliPixelDataInformation))]
    [KnownType(typeof(FileInformation))]
    public abstract class PixelDataInformation
    {

        /// <summary>
        /// Gets the type of the file (for e.g file or wado uri).
        /// </summary>
        [DataMember(Name = "PixelSource", EmitDefaultValue = false)]
        internal PixelSource PixelSource { get; set; }

        /// <summary>
        /// Gets or Sets the fileReadSize
        /// </summary>
        [DataMember(Name = "FileReadSize")]
        public int FileReadSize { get; internal set; }

        /// <summary>
        /// Gets or Sets the Compression information
        /// </summary>
        [DataMember(Name = "CompressionInformation", EmitDefaultValue = false)]
        public CompressionInformation CompressionInformation { get; set; }

        /// <summary>
        /// Gets or Sets the Image Conversion related information
        /// </summary>
        [DataMember(Name = "ImageConversionInformation", EmitDefaultValue = false)]
        public ImageConversionInformation ImageConversionInformation { get; set; }
        /// <summary>
        /// Gets or Sets whether clip the negative pixel data values to 0 when converting the
        /// signed pixel data to unsigned.
        /// </summary>
        [DataMember(Name = "ClipNegativePixelDataValuesToZero", EmitDefaultValue = false)]
        public bool ClipNegativePixelDataValuesToZero { get; set; }

        /// <summary>
        /// Gets or Sets whether the image to read is a multi-frame image.
        /// </summary>
        [DataMember(Name = "IsMultiframeImage", EmitDefaultValue = false)]
        public bool IsMultiframeImage { get; set; }

        /// <summary>
        /// Gets or Sets whether the image to read is a multi-frame image in one Call.
        /// </summary>
        [DataMember(Name = "ReadMultiframeInOneGo", EmitDefaultValue = false)]
        public bool ReadMultiframeInOneGo { get; set; }


        /// <summary>
        /// Gets or Sets the frame to read. The frame number is a 1-based index, meaning the first
        /// frame will have the frame number as 1.
        /// </summary>
        [DataMember(Name = "FrameNumber", EmitDefaultValue = false)]
        public int FrameNumber { get; internal set; }

        /// <summary>
        /// Gets whether to treat the given image as multi frame.
        /// </summary>
        /// <remarks>
        /// In cases where multiframe has to be read at one go, i.e.
        /// when <see cref="PixelDataInformation.ReadMultiframeInOneGo"/> is <see langword="true"/>, then
        /// though <see cref="PixelDataInformation.IsMultiframeImage"/> is <see langword="true"/>, the
        /// image should not be treated as MF image.
        /// </remarks>
        internal bool LoadOneFrameAtATime => IsMultiframeImage && !ReadMultiframeInOneGo;

        /// <summary>
        /// Gets a unique path or URL identifying the pixel data
        /// </summary>
        /// <returns></returns>
        internal abstract string GetItemPath();
    }

    /// <summary>
    /// Represents a pixel source which retrieves data from WADO endpoint
    /// </summary>
    [DataContract]
    public class WadoPixelDataInformation : PixelDataInformation
    {
        /// <summary>
        /// Gets the wado url path.
        /// </summary>
        [DataMember(Name = "WadoPath", EmitDefaultValue = false)]
        public string WadoPath { get; private set; }

        /// <summary>
        /// Gets the  additional web headers that includes the access token
        /// </summary>
        [DataMember(Name = "HttpHeaderAttributes", EmitDefaultValue = false)]
        public Dictionary<string, string> HttpHeaderAttributes { get; set; }


        /// <summary>
        /// Creates a file information with given data
        /// </summary>
        /// <param name="wadoPath">The wado url.</param>
        /// <param name="httpHeaderAttributes">contains the webheaders that includes the access token </param>
        public WadoPixelDataInformation(string wadoPath, Dictionary<string, string> httpHeaderAttributes)
        {
            WadoPath = wadoPath;
            PixelSource = PixelSource.WadoUri;
            HttpHeaderAttributes = httpHeaderAttributes;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        internal override string GetItemPath()
        {
            return WadoPath;
        }
    }

    /// <summary>
    /// Represents a pixel source which retrieves data from Ahli endpoint
    /// </summary>
    [DataContract]
    public class AhliPixelDataInformation : PixelDataInformation
    {
        /// <summary>
        /// Gets the Ahli complete URI path.
        /// </summary>
        [DataMember(Name = "AhliPath", EmitDefaultValue = false)]
        public string AhliPath { get; private set; }

        /// <summary>
        /// Unique store id of Amazon HealthLake Imaging
        /// </summary>
        [DataMember(Name = "DataStoreId", EmitDefaultValue = false)]
        public string DataStoreId { get; private set; }

        /// <summary>
        /// Unique id of series object in Amazon HealthLake Imaging
        /// </summary>
        [DataMember(Name = "ImageSetId", EmitDefaultValue = false)]
        public string ImageSetId { get; private set; }

        /// <summary>
        /// Unique id of series
        /// </summary>
        [DataMember(Name = "SeriesInstanceUid", EmitDefaultValue = false)]
        public string SeriesInstanceUid { get; private set; }

        /// <summary>
        /// Unique id of instance
        /// </summary>
        [DataMember(Name = "SopInstanceUid", EmitDefaultValue = false)]
        public string SopInstanceUid { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "HttpHeaderAttributes", EmitDefaultValue = false)]
        public Dictionary<string, string> HttpHeaderAttributes { get; set; }

        /// <summary>
        /// Frame Id of image
        /// </summary>
        [DataMember(Name = "FrameId", EmitDefaultValue = false)]
        public string FrameId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public AhliPixelDataInformation(
            string ahliPath,
            Dictionary<string, string> httpHeaderAttributes,
            string dataStoreId,
            string imageSetId,
            string seriesInstanceUid,
            string sopInstanceUid,
            string frameId
        )
        {
            AhliPath = ahliPath;
            PixelSource = PixelSource.Ahli;
            HttpHeaderAttributes = httpHeaderAttributes;
            DataStoreId = dataStoreId;
            ImageSetId = imageSetId;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            FrameId = frameId;
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        internal override string GetItemPath()
        {
            return AhliPath;
        }
    }

    /// <summary>
    /// This class holds the file information required by
    /// Memory manager. MM Client uses this class to pass the
    /// file information to server, hence this class is
    /// defined as DataContract
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    /// While reading from a file repository, set <c>ReadAttributesFromFile</c> to true, so that
    /// MemoryManagerServer will read the necessary compression and image conversion attributes
    /// from the file DICOM Header directly.
    /// </item>
    /// <item>
    /// While reading from IRC <br/>
    /// <list type="bullet">
    /// <item>
    /// If the file is compressed and decompression is required at the server then set
    /// <c>CompressionInformation</c> property with proper value.
    /// </item>
    /// <item>
    /// If signed to unsigned pixel data conversion is required then set 
    /// <c>ImageConversionInformation</c> with proper value.
    /// </item>
    /// </list>
    /// </item>
    /// </list>
    /// </remarks>
    [Serializable]
    [DataContract(Name = "FileInformation")]
    public sealed class FileInformation : PixelDataInformation
    {


        /// <summary>
        /// Creates a file information with given data
        /// </summary>
        /// <param name="fileName">name of the file</param>
        /// <param name="size">size of the read portion</param>
        /// <param name="offset">offset from where read will start</param>
        /// <param name="delayedFileLoad">
        /// Indicated whether File open must be delayed or not.</param>
        public FileInformation(string fileName, int size, long offset, bool delayedFileLoad)
        {
            FileName = fileName;
            // even if size comes as 0, accept it
            FileReadSize = size;
            FileReadOffset = offset;
            DelayedFileLoad = delayedFileLoad;
            PixelSource = PixelSource.File;
        }

        /// <summary>
        /// Creates a file information with given data
        /// </summary>
        /// <param name="fileName">name of the file</param>
        /// <param name="size">size of the read portion</param>
        /// <param name="offset">offset from where read will start</param>
        public FileInformation(string fileName, int size, long offset) : this(fileName, size, offset, true)
        {
        }

        /// <summary>
        /// Gets or Sets the filename
        /// </summary>
        [DataMember(Name = "FileName")]
        public string FileName { get; internal set; }

        /// <summary>
        /// Gets or Sets the fileReadOffset
        /// </summary>
        [DataMember(Name = "FileReadOffset")]
        public long FileReadOffset { get; internal set; }



        /// <summary>
        /// Gets or Sets whether to read the compression and image conversion related information
        /// from the file. This has to be set to true for reading from DCM Files.
        /// </summary>
        [DataMember(Name = "ReadAttributesFromFile", EmitDefaultValue = false)]
        public bool ReadAttributesFromFile { get; set; }


        /// <summary>
        /// Gets or sets whether the file being opened contains multiple bulks.
        /// </summary>
        [DataMember(Name = "IsMultiBulkFile")]
        public bool IsMultipleBulkFile { get; internal set; }

        /// <summary>
        /// Gets or sets whether the file being opened contains multiple bulks.
        /// </summary>
        [DataMember(Name = "DelayedFileLoad")]
        public bool DelayedFileLoad { get; set; }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        internal override string GetItemPath()
        {
            return FileName;
        }
    }
}
