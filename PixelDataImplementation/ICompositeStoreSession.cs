using Philips.Platform.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DicomObject = Philips.Platform.Dicom.Data.DicomObject;

namespace PixelDataImplementation
{
    /// <summary>
    /// Contains methods to perform store operations in a store session.
    /// </summary>
    internal interface ICompositeStoreSession
    {

        /// <summary>
        /// Stores given dicom file into the database.
        /// </summary>
        /// <param name="compositeFileName">the composite file name</param>
        void Store(string compositeFileName);

        /// <summary>
        /// Stores the given composite dicom object
        /// </summary>
        /// <param name="compositeDicomObject"></param>
        void Store(DicomObject compositeDicomObject);

        /// <summary>
        /// Stores imageHeader and the pixel data in the device.
        /// </summary>
        /// <param name="imageHeader">
        /// the image header
        /// </param>
        /// <param name="pixelDataIntPtr">
        /// Pointer to the pixel data in memory corresponding to the image
        /// </param>
        /// <param name="pixelDataLength">the length of pixel data</param>
        void Store(DicomObject imageHeader, IntPtr pixelDataIntPtr, int pixelDataLength);

        /// <summary>
        /// Stores the given Non dicom object
        /// </summary>
        /// <param name="compositeDicomObject">Header containing study series info</param>
        /// <param name="nonDicomfilePath">full path of non dicom object</param>
        void StoreNonDicom(DicomObject compositeDicomObject, string nonDicomfilePath);

        /// <summary>
        /// Stores imageHeader, bulk data references and dicom file to database.
        /// </summary>
        /// <param name="imageHeader">
        /// the image header without pixel data
        /// </param>
        /// <param name="bulkDataReferences">
        /// Collection of pixel data references to composite file.
        /// <remarks>
        /// Can be null but performant when bulk data references are provided for images
        /// </remarks>
        /// </param>
        /// <param name="compositeFileName">Dicom composite file name</param>
        /// <param name="pixelOffset">offset at which the pixel data starts</param> 
        /// <param name="pixelLength">pixel length of the image</param>
        void Store(DicomObject imageHeader, Collection<BulkDataReference> bulkDataReferences,
            string compositeFileName, long pixelOffset, long pixelLength);

        /// <summary>
        /// Commits the previous stores.
        /// </summary>
        void Commit();

        /// <summary>
        /// Updates the image by passing information through the imageHeader as tag value pairs.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <param name="delta">The delta.</param>
        void Update(Identifier identifier, DicomObject delta);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="studyDicomObject"></param>
        void StoreStudy(DicomObject studyDicomObject);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seriesDicomObject"></param>
        void StoreSeries(DicomObject seriesDicomObject);
    }

}
