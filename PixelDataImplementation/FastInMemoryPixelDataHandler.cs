using Philips.Platform.Common;
using Philips.Platform.StorageDevices.Abstractions;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// Provide APIs to load pixel from fast repository device.
    /// </summary>
    internal interface IFastInMemoryPixelDataHandler
    {
        /// <summary>
        /// Load pixel data from fast repository device.
        /// </summary>
        /// <param name="pixelInfo"><see cref="InMemoryPixelInformation"/></param>
        /// <returns>Pointer to associated image pixels.</returns>
        IntPtr LoadPixel(InMemoryPixelInformation pixelInfo);

        /// <summary>
        /// Image pixel description
        /// </summary>
        /// <param name="imageDicomObject">Image dicom object.</param>
        /// <returns>Provides information about the pixel attributes such as rows, columns bits stored
        /// ,bits allocated etc.<see cref="ImagePixelDescription"/>
        /// </returns>
        ImagePixelDescription GetImagePixelDescription(DicomObject imageDicomObject);

        /// <summary>
        /// Opens an existing memory-mapped file that has the specified name in system memory.
        /// </summary>
        /// <param name="mmfName">Name of memory mapped file.</param>
        void OpenExistingMmf(string mmfName);

        /// <summary>
        /// Load pixel data from fast repository device using opened Mmf.
        /// <remarks>Call to this API should be made only after calling <see cref="OpenExistingMmf"/></remarks>
        /// </summary>
        /// <param name="pixelInfo"><see cref="InMemoryPixelInformation"/></param>
        /// <returns>Pointer to associated image pixels.</returns>
        IntPtr LoadPixelFromExistingMmf(InMemoryPixelInformation pixelInfo);

        /// <summary>
        /// Release resources.
        /// </summary>
        void ReleaseResources();
    }

    /// <summary>
    /// Provides an implementation of <see cref="IFastInMemoryPixelDataHandler"/>
    /// </summary>
    // @AdapterType: Threading
    internal class FastInMemoryPixelDataHandler : IFastInMemoryPixelDataHandler
    {
        private MemoryMappedFile mmf;
        private MemoryMappedViewStream stream;
        /// <summary>
        /// <see cref="IFastInMemoryPixelDataHandler.LoadPixel"/>
        /// </summary>
        public IntPtr LoadPixel(InMemoryPixelInformation pixelInfo)
        {
            OpenExistingMmf(pixelInfo.PixelMmfName);
            return LoadPixelFromExistingMmf(pixelInfo);
        }

        /// <summary>
        /// <see cref="IFastInMemoryPixelDataHandler.OpenExistingMmf"/>
        /// </summary>
        public void OpenExistingMmf(string mmfName)
        {
            mmf = MemoryMappedFile.OpenExisting(mmfName);
        }

        /// <summary>
        /// <see cref="IFastInMemoryPixelDataHandler.LoadPixelFromExistingMmf"/>
        /// </summary>
        public IntPtr LoadPixelFromExistingMmf(InMemoryPixelInformation pixelInfo)
        {
            stream = mmf.CreateViewStream(pixelInfo.PixelOffset, pixelInfo.PixelLength);
            return stream.SafeMemoryMappedViewHandle.DangerousGetHandle();
        }

        /// <summary>
        /// <see cref="IFastInMemoryPixelDataHandler.GetImagePixelDescription"/>
        /// </summary>
        public ImagePixelDescription GetImagePixelDescription(DicomObject imageDicomObject)
        {
            var imageConversionInformation = new ImageConversionInformation(imageDicomObject);
            var imageTranslator = new ImageTranslator(imageConversionInformation, imageDicomObject, true);
            imageConversionInformation = imageTranslator.GetModifiedImageConversionInformation();
            PlanarConfiguration planarConfig =
                GetPlanarConfiguration(imageConversionInformation.PlanarConfiguration);
            bool isImageConverted =
                imageConversionInformation.ImageConversionType != ImageConversions.None;
            return ImagePixelDescriptionHelper.GetImagePixelDescription(
                planarConfig,
                isImageConverted,
                imageConversionInformation);
        }

        /// <summary>
        /// <see cref="IFastInMemoryPixelDataHandler.ReleaseResources"/>
        /// </summary>
        public void ReleaseResources()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
            if (mmf != null)
            {
                mmf.Dispose();
                mmf = null;
            }
        }

        /// <summary>
        /// Gets the planar configuration.
        /// </summary>
        /// <param name="planarConfiguration">The planar configuration.</param>
        /// <returns>PlanarConfiguration</returns>
        private static PlanarConfiguration GetPlanarConfiguration(ReadOnlyArray<ushort> planarConfiguration)
        {
            PlanarConfiguration planarConfig = PlanarConfiguration.PixelInterleaved;
            if (planarConfiguration.Count > 0 && planarConfiguration[0] == 1)
            {
                planarConfig = PlanarConfiguration.PlanarInterleaved;
            }
            return planarConfig;
        }
    }
}
