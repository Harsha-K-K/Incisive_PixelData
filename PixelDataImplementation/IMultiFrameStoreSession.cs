using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DicomObject = Philips.Platform.Dicom.Data.DicomObject;

namespace PixelDataImplementation
{
    internal interface IMultiFrameStoreSession
    {

        /// <summary>
        /// Method to add the frame to the Multi-Frame Image associated with this session.
        /// </summary>
        /// <param name="frameDicomObject">
        /// The <see cref="DicomObject"/> contains all information 
        /// about the frame.
        /// </param>
        /// <param name="pixelDataPtr">
        /// Pointer to the pixel data in memory corresponding to the frame.
        /// </param>
        /// <param name="pixelDataLength">the length of pixel data</param>
        void StoreFrame(
            DicomObject frameDicomObject,
            IntPtr pixelDataPtr,
            long pixelDataLength);

        /// <summary>
        /// Method to add the frame to the Multi-Frame Image associated with this session.
        /// </summary>
        /// <param name="pixelDataPtr">
        /// Pointer to the pixel data in memory corresponding to the frame.
        /// </param>
        /// <param name="pixelDataLength">the length of pixel data</param>
        void StoreFramePixelData(IntPtr pixelDataPtr, long pixelDataLength);

        /// <summary>
        /// Method to finalize the store operations. 
        /// This method starts persisting the data into storage device(s) 
        /// and makes it available to other applications.
        /// </summary>
        void Commit();

        /// <summary>
        ///  Discard all the operations. 
        ///  This method cannot be called once FinalizeSessionAsync() is called.
        /// </summary>
        void DiscardSession();
    }
}
