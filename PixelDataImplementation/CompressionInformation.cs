using Philips.Platform.ApplicationIntegration.DataAccess;
using Philips.Platform.Common;

using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace PixelDataImplementation
{
    /// <summary>
    /// Holds image compression related information for memoryManager
    /// </summary>
    [Serializable]
    [DataContract]
    public sealed class CompressionInformation
    {

        #region Private members

        private Collection<short> myPlanarConfiguration;
        private string myFormat;
        private string myPhotometricInterpretation;
        private int myBitsAllocated;
        private int myDecompressedLength;
        private bool myHasPixels;
        private bool myMustComputeCompressedFrameLength;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates CompressionInformation from the given dicom object.
        /// </summary>
        /// <param name="theDicomObject">
        /// The dicom object from which the compression information has to be built from.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when any of the attributes is not present in the given dicom object.
        /// </exception>
        /// <remarks>
        /// When using this constrcutor to construct the CompressionInformation and then to set the
        /// resultant CompressionInformation in FileInformation, make sure to check
        /// CompressionInformation.Format for null or empty. e.g:
        /// <code>
        /// FileInformation fi = new FileInformation(...);
        /// DicomObject dicomObject = ...;
        /// CompressionInformation ci = new CompressionInformation(dicomObject);
        /// fi.CompressionInformation = 
        ///     (
        ///         (ci.HasPixels) &amp;&amp; 
        ///         (
        ///             !string.IsNullOrEmpty(ci.Format) &amp;&amp; 
        ///             IsCompressedTransferSyntax(ci.Format)
        ///         )
        ///     ) ? 
        ///     ci : null;
        /// </code>
        /// <para>
        /// Where <c>IsCompressedTransferSyntax</c></para> tells whether the <c>Format</c> is really
        /// a compressed one.
        /// <para>
        /// The <c>Format</c> needs to be checked whether it is really a compressed transfer
        /// syntax (IsCompressedTransferSyntax). Only if so, compression ifnormation should be 
        /// set in <c>FileInformation.</c>
        /// </para>
        /// </remarks>
        public CompressionInformation(DicomObject theDicomObject)
        {
            GetCompressionInformation(theDicomObject);
        }

        /// <summary>
        /// Creates CompressionInformation from the given dicom object.
        /// </summary>
        /// <param name="theDicomObject">
        /// The dicom object from which the compression information has to be built from.
        /// </param>
        /// <param name="useNumberOfFramesInCalculations">
        /// Specifies whether to use the number of frames in pixel data length
        /// calculations.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when any of the attributes is not present in the given dicom object.
        /// </exception>
        /// <remarks>
        /// When using this constrcutor to construct the CompressionInformation and then to set the
        /// resultant CompressionInformation in FileInformation, make sure to check
        /// CompressionInformation.Format for null or empty. e.g:
        /// <code>
        /// FileInformation fi = new FileInformation(...);
        /// DicomObject dicomObject = ...;
        /// CompressionInformation ci = new CompressionInformation(dicomObject);
        /// fi.CompressionInformation = 
        ///     (
        ///         (ci.HasPixels) &amp;&amp; 
        ///         (
        ///             !string.IsNullOrEmpty(ci.Format) &amp;&amp; 
        ///             IsCompressedTransferSyntax(ci.Format)
        ///         )
        ///     ) ? 
        ///     ci : null;
        /// </code>
        /// <para>
        /// Where <c>IsCompressedTransferSyntax</c></para> tells whether the <c>Format</c> is really
        /// a compressed one.
        /// <para>
        /// The <c>Format</c> needs to be checked whether it is really a compressed transfer
        /// syntax (IsCompressedTransferSyntax). Only if so, compression ifnormation should be 
        /// set in <c>FileInformation.</c>
        /// </para>
        /// </remarks>
        public CompressionInformation(
            DicomObject theDicomObject,
            bool useNumberOfFramesInCalculations
        )
        {
            GetCompressionInformation(theDicomObject, useNumberOfFramesInCalculations);
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="theFormat">The format.</param>
        /// <param name="theBitsAllocated">The bits allocated.</param>
        /// <param name="thePhotometricInterpretation">The photometric interpretation.</param>
        /// <param name="thePlanarConfiguration">The planar configuration.</param>
        /// <param name="theDecompressedLength">Length of the decompressed.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when any of the attributes is not present in the given dicom object.
        /// </exception>
        public CompressionInformation(
            string theFormat,
            int theBitsAllocated,
            string thePhotometricInterpretation,
            Collection<short> thePlanarConfiguration,
            int theDecompressedLength
        )
        {
            if (string.IsNullOrEmpty(theFormat))
            {
                //LogHelper.DevelopmentErrorLog(
                //    ModuleIDs.MemoryManagerClient,
                //    EventIDs.Error,
                //    "Compression format cannot be null or empty."
                //);
                throw new ArgumentException(
                    "Compression format cannot be null or empty.",
                    "theFormat"
                );
            }
            Format = theFormat;
            BitsAllocated = theBitsAllocated;
            PhotometricInterpretation = thePhotometricInterpretation;
            PlanarConfiguration = thePlanarConfiguration;
            DecompressedLength = theDecompressedLength;
            HasPixels = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Get the compression format. If the image is not compressed or if the compression format 
        /// is other t
        /// </summary>
        [DataMember(Name = "Format", Order = 1)]
        public string Format
        {
            get { return myFormat; }
            internal set { myFormat = value; }
        }

        /// <summary>
        /// Get the bits allocated
        /// </summary>
        [DataMember(Name = "BitsAllocated", Order = 2)]
        public int BitsAllocated
        {
            get { return myBitsAllocated; }
            internal set { myBitsAllocated = value; }
        }

        /// <summary>
        /// Get the photometric interpretation
        /// </summary>
        [DataMember(Name = "PhotometricInterpretation", Order = 3)]
        public string PhotometricInterpretation
        {
            get { return myPhotometricInterpretation; }
            internal set { myPhotometricInterpretation = value; }
        }

        /// <summary>
        /// Get the planar configuration
        /// </summary>
        [DataMember(Name = "PlanarConfiguration", Order = 4)]
        public Collection<short> PlanarConfiguration
        {
            get { return myPlanarConfiguration; }
            internal set { myPlanarConfiguration = value; }
        }

        /// <summary>
        /// Get the decompressed length
        /// </summary>
        [DataMember(Name = "DecompressedLength", Order = 5)]
        public int DecompressedLength
        {
            get { return myDecompressedLength; }
            internal set { myDecompressedLength = value; }
        }

        /// <summary>
        /// Gets whether the data has got pixels.
        /// </summary>
        [DataMember(Name = "HasPixels", Order = 6)]
        public bool HasPixels
        {
            get { return myHasPixels; }
            internal set { myHasPixels = value; }
        }

        /// <summary>
        /// Gets whether the data has got pixels.
        /// </summary>
        [DataMember(Name = "MustComputeCompressedFrameLength", Order = 7)]
        public bool MustComputeCompressedFrameLength
        {
            get { return myMustComputeCompressedFrameLength; }
            internal set { myMustComputeCompressedFrameLength = value; }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the compression information.
        /// </summary>
        /// <param name="theDicomObject">The dicom object.</param>
        /// <param name="useNumberOfFrames">
        /// specifies whether to use number of frames during
        /// pixel data length calculation.
        /// <br/>Default value: false.
        /// </param>
        private void GetCompressionInformation(
            DicomObject theDicomObject,
            bool useNumberOfFrames = false
        )
        {
            string format = theDicomObject.GetString(DicomDictionary.DicomTransferSyntaxUid);

            if (string.IsNullOrEmpty(format))
            {
                return;
            }
            ushort? ba = theDicomObject.GetUInt16(DicomDictionary.DicomBitsAllocated);
            ushort? rs = theDicomObject.GetUInt16(DicomDictionary.DicomRows);
            ushort? cs = theDicomObject.GetUInt16(DicomDictionary.DicomColumns);
            ushort? spp = theDicomObject.GetUInt16(DicomDictionary.DicomSamplesPerPixel);
            if (ba == null || rs == null || cs == null || spp == null)
            {
                // non-image data, nothing to uncompress
                HasPixels = false;
                return;
            }

            // all the below attributes are of type 1, hence no need to check for null
            ushort bitsAlloc = ba.Value;
            ushort rows = rs.Value;
            ushort columns = cs.Value;

            ushort samplesPerPixel = spp.Value;

            string pi = theDicomObject.GetString(DicomDictionary.DicomPhotometricInterpretation);

            int decompressedLength = rows * columns * ((bitsAlloc + 7) / 8) * samplesPerPixel;
            if (useNumberOfFrames)
            {
                int? numberOfFrames = theDicomObject.GetInt32(DicomDictionary.DicomNumberOfFrames);
                if (numberOfFrames.HasValue)
                {
                    decompressedLength *= numberOfFrames.Value;
                }
            }
            DecompressedLength = decompressedLength;
            Format = format;
            BitsAllocated = bitsAlloc;
            PhotometricInterpretation = pi;


            PlanarConfiguration = GetPlanarConfiguration(theDicomObject);
            HasPixels = true;
        }

        /// <summary>
        /// Gets the planar configuration.
        /// </summary>
        /// <param name="theDicomObject">The dicom object.</param>
        /// <returns></returns>
        private static Collection<short> GetPlanarConfiguration(DicomObject theDicomObject)
        {
            ushort samplesPerPixel =
                theDicomObject.GetUInt16(DicomDictionary.DicomSamplesPerPixel).Value;
            if (samplesPerPixel == 1)
            {
                return new Collection<short>() { 0 };
            }
            ushort? planarConfig = theDicomObject.GetUInt16(
                DicomDictionary.DicomPlanarConfiguration);
            if (!planarConfig.HasValue)
            {
                string error = "The given dicom object doesnot contain mandatory planar" +
                    " configuration attribute.Defaulting to 0";
                //LogHelper.LogInfoAlways(error);
                //PR#ISP00137841. Images with no planar
                //configuration needs to be defaulted to 0
                //as per ISP impementation
                planarConfig = 0;
            }
            return new Collection<short>() { (short)planarConfig.Value };
        }
        #endregion
    }

}