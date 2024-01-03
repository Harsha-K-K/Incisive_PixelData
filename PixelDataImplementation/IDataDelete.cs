using Philips.Platform.Common;
using Philips.Platform.StorageDevicesClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Philips.Platform.ApplicationIntegration.Decoupling;


namespace PixelDataImplementation
{
    /// <summary>
    /// Methods for the Delete functionality.
    /// </summary>    
    internal interface IDataDelete
    {

        /// <summary>
        /// Deletes the data at the requested level.
        /// </summary>
        /// <param name="level">Level can be Patient, Study, Series or Image</param>
        /// <param name="identifier">The identifier</param>
        void DeleteData(Level level, Identifier identifier);

        /// <summary>
        /// Deletes a collection of the specified images. All the images have to be under
        /// the same series.
        /// </summary>
        /// <param name="imageIdentifiers">
        /// The list of images to delete.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Not all the images are under the same series (or) non-image
        /// identifiers are passed as input.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The input is null.
        /// </exception>
        void ForceDeleteImagesUnderSeries(IList<Identifier> imageIdentifiers);

        /// <summary>
        /// Deletes the data at the requested level even if delete protection is applied.
        /// </summary>
        /// <param name="level">Level can be Patient, Study, Series or Image</param>
        /// <param name="identifier">The identifier</param>
        /// <param name="propagateDeleteUpwards">Indicates if the delete will be propagated upwards 
        /// if the last data at this level is deleted.</param>
        void ForceDeleteData(Level level, Identifier identifier, bool propagateDeleteUpwards);

        /// <summary>
        /// Protect a study from deletion.
        /// </summary>
        /// <param name="studyKey">
        /// The storagekey representing the study to be delete protected.
        /// </param>
        /// <returns>
        /// A token for the delete protection. This token should be used while
        /// delete unprotecting the study.
        /// </returns>
        string DeleteProtectStudy(StorageKey studyKey);

        /// <summary>
        /// Set Study attibute to Protect the Study from Deletion.
        /// </summary>
        /// <param name="studyKey">
        /// The storagekey representing the study to be delete protected.
        /// </param>
        void LockStudy(StorageKey studyKey);

        /// <summary>
        /// Set Study attibute to UnProtect the Study for Deletion.
        /// </summary>
        /// <param name="studyKey">
        /// The storagekey representing the study to be delete protected.
        /// </param>
        void UnlockStudy(StorageKey studyKey);

        /// <summary>
        /// Checks whether the given study is delete protected.
        /// </summary>
        /// <param name="studyKey">
        /// The storagekey representing the study to be delete protected.
        /// </param>
        /// <returns>
        /// A boolean indicating if the study is delete protected.
        /// </returns>
        bool IsStudyLocked(StorageKey studyKey);

        /// <summary>
        /// Protect Series from Deletion.
        /// </summary>
        /// <param name="seriesKey">
        /// The storagekey representing the series to be delete protected.
        /// </param>
        /// <returns>
        /// A token for the delete protection. This token should be used while
        /// delete unprotecting the series.
        /// </returns>
        string DeleteProtectSeries(StorageKey seriesKey);

        /// <summary>
        /// UnProtect study from DeleteProtection.
        /// </summary>
        /// <param name="studyKey">
        /// The study for which delete protection is to be removed.
        /// </param>
        /// <param name="deleteProtectToken">
        /// The token returned while delete protecting the study.
        /// </param>
        void DeleteUnProtectStudy(StorageKey studyKey, string deleteProtectToken);

        /// <summary>
        /// Checks whether the given series is delete protected.
        /// </summary>
        /// <param name="seriesKey">
        /// The storage key for the series.
        /// </param>
        /// <returns>
        /// A boolean indicating if the series is delete protected.
        /// </returns>
        bool IsSeriesDeleteProtected(StorageKey seriesKey);

        /// <summary>
        /// Checks whether the given study is delete protected.
        /// </summary>
        /// <param name="studyKey">
        /// The storage key for the study.
        /// </param>
        /// <returns>
        /// A boolean indicating if the study is delete protected.
        /// </returns>
        bool IsStudyDeleteProtected(StorageKey studyKey);

        /// <summary>
        /// UnProtect series from DeleteProtection.
        /// </summary>
        /// <param name="seriesKey">
        /// The series for which delete protection is to be removed.
        /// </param>
        /// <param name="deleteProtectToken">
        /// The token returned while delete protecting the series.
        /// </param>
        void DeleteUnprotectSeries(StorageKey seriesKey, string deleteProtectToken);

        /// <summary>
        /// Determines whether this instance can delete the specified identifier.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        bool CanDelete(Identifier identifier);

        /// <summary>
        /// Protect a patient from deletion.
        /// </summary>
        /// <param name="patientKey">
        /// The storagekey representing the patient to be delete protected.
        /// </param>
        /// <returns>
        /// A token for the delete protection. This token should be used while
        /// delete unprotecting the patient.
        /// </returns>
        string DeleteProtectPatient(StorageKey patientKey);

        /// <summary>
        /// UnProtect patient from DeleteProtection.
        /// </summary>
        /// <param name="patientKey">
        /// The patient for which delete protection is to be removed.
        /// </param>
        /// <param name="deleteProtectToken">
        /// The token returned while delete protecting the patient.
        /// </param>
        void DeleteUnProtectPatient(StorageKey patientKey, string deleteProtectToken);

        /// <summary>
        /// Checks whether the given patient is delete protected.
        /// </summary>
        /// <param name="patientKey">
        /// The storage key for the patient.
        /// </param>
        /// <returns>
        /// A boolean indicating if the patient is delete protected.
        /// </returns>
        bool IsPatientDeleteProtected(StorageKey patientKey);

        /// <summary>
        /// checks whether the given device is delete protected
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        bool IsDeviceDeleteProtected(string deviceId);

        /// <summary>
        /// See <see cref=" DeleteManagerBase.DeleteNonImages(StorageKeyCollection)"/>
        /// </summary>
        /// <param name="identifiers">Identifier list of the non image objects to be deleted</param>
        /// <returns></returns>
        void ForceDeleteNonImageData(List<Identifier> identifiers);
    }
}
