using Philips.Platform.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// Provides access to images on the memory manager client.
    /// </summary>
    /// <remarks>
    /// For convenience, IMMU or MMU is used as a synonym for IMemoryMappedUnit at various places.
    /// </remarks>
    [Obsolete("DEPRECATED API USED", false)]
    public interface IMemoryMappedUnit : IDisposable
    {
        /// <summary>
        /// Prefetches the pixel data in memory so that the pixel data can be accessed fastly when
        /// needed. 
        /// </summary>
        /// <remarks>
        /// Prefetch signals the memory manager that the data will be required soon via the
        /// <see cref="Lock"/> method.
        /// </remarks>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while prefetching the MMU.
        /// </exception>
        /// <exception cref="FailException">
        /// Thrown when any other error other than those specified above occurs while prefetching 
        /// the MemoryMappedUnit.
        /// </exception>
        /// <seealso cref="CancelPreFetch"/>
        void PreFetch();

        /// <summary>
        /// Cancels an in-progress prefetch operation on current memory mapped unit. 
        /// </summary>
        /// <remarks>
        /// This is just a hint to the memory manager.
        /// </remarks>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while cancelling the prefetch
        /// of the current MemoryMappedUnit.
        /// </exception>
        /// <exception cref="FailException">
        /// Thrown when any other error other than those specified above occurs while cancelling 
        /// the prefetch of the current MemoryMappedUnit.
        /// </exception>
        /// <seealso cref="PreFetch"/>
        void CancelPreFetch();

        /// <summary>
        /// Returns the actual bulk data that is pointed to by this cached object. 
        /// This data remains valid until an <see cref="Unlock"/> call is made on this object.
        /// </summary>
        /// <param name="size">Size of the MMU.</param>
        /// <returns>
        /// The bulk data represented by this object.
        /// </returns>
        /// <remarks>
        /// The Lock/Unlock/UnlockAndClean sequence are reference counted. 
        /// This means that calling Unlock will unlock the data only 
        /// after calling at as many times as Lock was called. Lock may 
        /// be called any number of times, and will always return the same object.
        /// </remarks>
        /// <exception cref="InsufficientMemoryException">
        /// Thrown when there is no enough memory to map the pixel data into the process.
        /// </exception>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while locking the 
        /// MemoryMappedUnit.
        /// </exception>
        /// <exception cref="FailException">
        /// Thrown when any other error other than those specified above occurs while locking the
        /// MemoryMappedUnit.
        /// </exception>
        /// <seealso cref="Unlock"/>
        /// <seealso cref="UnlockAndClean"/>
        /// <seealso cref="MarkForCleanup"/>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1021:AvoidOutParameters",
            Justification = "This is by design."
        )]
        IntPtr Lock(out int size);

        /// <summary>
        /// This call synchrnously waits for the MemoryMapped files
        /// to be loaded in the server side
        /// <exception cref="InsufficientMemoryException">
        /// Thrown when there is no enough memory to map the pixel data into the process.
        /// </exception>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while locking the 
        /// MemoryMappedUnit.
        /// </exception>
        /// <exception cref="FailException">
        /// Thrown when any other error other than those specified above occurs while locking the
        /// MemoryMappedUnit.
        /// </exception>
        /// </summary>
        void LoadPixels();

        /// <summary>
        /// Unlocks the MemoryMappedUnit. <br/> 
        /// Call <see cref="UnlockAndClean"/> if release of mapped memory is also required.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Note that the Unlock method only decrements the internal reference count. It will not
        /// release the mapped memory even if the reference count reaches 0.
        /// </para>
        /// <para>
        /// The Lock/Unlock/UnlockAndClean sequence are reference counted. <br/>
        /// This means that calling Unlock will unlock the data only after calling at as many times
        /// as Lock was called. Lock may be called any number of times, and will always return
        /// the same object. After calling UnlockAndClean you should not access the data 
        /// previously returned by a call to <see cref="Lock"/>.
        /// </para>
        /// <para>
        /// Note that when the MemoryManager server is running inproc, i.e. in the same process as
        /// the client, then calling <see cref="Unlock"/> is equivalent to calling
        /// <see cref="UnlockAndClean"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while unlocking the 
        /// MemoryMappedUnit.
        /// </exception>
        /// <exception cref="FailException">
        /// Thrown when any error occurs while unlocking the MemoryMappedUnit.
        /// </exception>
        /// <seealso cref="Lock"/>
        /// <seealso cref="UnlockAndClean"/>
        /// <seealso cref="MarkForCleanup"/>
        void Unlock();

        /// <summary>
        /// Unlocks the MemoryMappedUnit and releases the mapped memory optionally. <br/> 
        /// Call <see cref="Unlock"/> if release of mapped memory is not required.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is similar to Unlock except that this will also release the mapped memory
        /// if the internal reference count reaches 0.
        /// </para>
        /// <para>
        /// The Lock/Unlock/UnlockAndClean sequence are reference counted. <br/> 
        /// This means that calling Unlock will unlock the data only after calling at as many times
        /// as Lock was called. Lock may be called any number of times, and will always return
        /// the same object. After calling UnlockAndClean you should not access the data 
        /// previously returned by a call to <see cref="Lock"/>.
        /// </para>
        /// <para>
        /// Note that when the MemoryManager server is running inproc, i.e. in the same process as
        /// the client, then calling <see cref="Unlock"/> is equivalent to calling
        /// <see cref="UnlockAndClean"/>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// true if cleanup is successfull, false otherwise.
        /// </returns>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while unlocking the 
        /// MemoryMappedUnit.
        /// </exception>
        /// <exception cref="FailException">
        /// Thrown when any error occurs while unlocking the MemoryMappedUnit.
        /// </exception>
        /// <seealso cref="Lock"/>
        /// <seealso cref="Unlock"/>
        /// <seealso cref="MarkForCleanup"/>
        bool UnlockAndClean();

        /// <summary>
        /// Makes the current MemoryMappedUnit eligible for cleanup if there are no active locks
        /// held.
        /// </summary>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while marking the 
        /// MemoryMappedUnit for cleanup.
        /// </exception>
        /// <exception cref="FailException">
        /// Thrown when any error occurs while marking the 
        /// MemoryMappedUnit for cleanup.
        /// </exception>
        /// <seealso cref="Lock"/>
        /// <seealso cref="Unlock"/>
        /// <seealso cref="UnlockAndClean"/>
        void MarkForCleanup();

        /// <summary>
        /// Gets the name of memory mapping associated with the file object.
        /// </summary>
        string MemoryMappedUnitName { get; }

        /// <summary>
        /// Gets the file name for this MMU
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Gets a value indicating if the object is available in memory
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Gets a value indicating if the object is available on client side
        /// </summary>
        bool IsReadyOnClient { get; }

        /// <summary>
        /// Gets a boolean value indicating if the object is readonly.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Specifies whether the MemoryMappedUnit belongs to a frame in a multi frame image.
        /// </summary>
        bool IsFrame { get; }

        /// <summary>
        /// Gets the modified image conversion information.
        /// </summary>
        /// <returns>
        /// <list type="bullet">
        /// <item>
        /// If the image conversion information was passed as null in FileInformation while opening
        /// the file, then this returns null.
        /// </item>
        /// <item>
        /// If image conversion has actually happened, this will return the modified image
        /// conversion information.
        /// </item>
        /// <item>
        /// If tje image conversion hasn't happened or was not needed, then this returns the
        /// image conversion information that was provided by client in FileInformation during
        /// OpenFile, as is.
        /// </item>
        /// </list>
        /// </returns>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while getting the image
        /// information.
        /// </exception>
        /// <exception cref="FailException">
        /// Thrown when any other error other than those specified above occurs while getting the
        /// image information.
        /// </exception>
        ImageConversionInformation ModifiedImageConversionInformation { get; }

        /// <summary>
        /// Gets the Full dicom object for the given MMU. 
        /// Note that the returned dicom object will not have pixel data and pixel data has to 
        /// be read separately using <see cref="Lock"/>.
        /// <br/>
        /// <list>
        /// <item>
        /// If this memory mapped unit belongs to a frame, i.e. when <see cref="IsFrame"/> is true,
        /// then this DicomObject returns the dicom header belonging to this particular frame.<br/>
        /// The returned header in case of frame contains all Multiframe image attributes,
        /// SharedFunctionalGroup and PerFrameFunctionalGroup of this particular frame if one
        /// exists.
        /// </item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// In case of SF images, if the image is not <see cref="Lock">Lock'ed</see> 
        /// yet, then the DicomObject will be created 
        /// directly by deserializing the DICOM file given during 
        /// <see cref="MemoryManagerClient.OpenFile(PixelDataInformation,bool,bool)"/>
        /// method. 
        /// <br/>
        /// If the image is already Lock'ed then the DICOM Object will be created
        /// from the data that was shared by server. <br/> 
        /// </remarks>
        /// <returns>
        /// DicomObject holding the attributes (except Pixel Data) present in the given DICOM File.
        /// <br/><para>This returns null if the file that was opened was not a complete dicom file, 
        /// e.g. only pixel data (bulk) file.</para>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the MemoryMappedUnit is not created out of a dicom file.
        /// </exception>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while getting the dicom object.
        /// </exception>
        /// <exception cref="FailException">
        /// Thrown when any other error other than those specified above occurs while getting the
        /// dicom object.
        /// </exception>
        DicomObject DicomObject { get; }

        /// <summary>
        /// Gets the Full dicom object for the given MMU. 
        /// Note that the returned dicom object will not have pixel data and pixel data has to 
        /// be read separately using <see cref="Lock"/>.
        /// <br/>
        /// <list>
        /// <item>
        /// If this memory mapped unit belongs to a frame, i.e. when <see cref="IsFrame"/> is true,
        /// then this DicomObject returns the dicom header belonging to this particular frame.<br/>
        /// The returned header in case of frame contains all Multiframe image attributes,
        /// SharedFunctionalGroup and PerFrameFunctionalGroup of this particular frame if one
        /// exists.
        /// </item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// In case of SF images, if the image is not <see cref="Lock">Lock'ed</see> or 
        /// <see cref="PreFetch">Prefetch'ed</see> yet, then the DicomObject will be created 
        /// directly by deserializing the DICOM file given during 
        /// <see cref="MemoryManagerClient.OpenFile(PixelDataInformation,bool,bool)"/>
        /// method. 
        /// <br/>
        /// If the image is already Lock'ed or Prefetch'ed then the DICOM Object will be created
        /// from the data that was shared by server. <br/> 
        /// </remarks>
        /// <returns>
        /// DicomObject holding the attributes (except Pixel Data) present in the given DICOM File.
        /// <br/><para>This returns null if the file that was opened was not a complete dicom file, 
        /// e.g. only pixel data (bulk) file.</para>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the MemoryMappedUnit is not created out of a dicom file.
        /// </exception>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while getting the dicom object.
        /// </exception>
        /// <exception cref="FailException">
        /// Thrown when any other error other than those specified above occurs while getting the
        /// dicom object.
        /// </exception>
        DicomObject PreloadedDicomObject { get; }

        /// <summary>
        /// This method is the same as the <see cref="Lock"/> method, except that it returns a 
        /// pointer to the beginning of the DICOM message in memory, instead of only the pixel data.
        /// </summary>
        /// <param name="msgLen">Dicom header length.</param>
        /// <param name="pixelDataOffset">pixel offset</param>
        /// <returns>DICOM data related to the respective DICOM message</returns>
        /// <exception cref="InsufficientMemoryException">
        /// Thrown when there is no enough memory to map the pixel data into the process.
        /// </exception>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while locking the 
        /// MemoryMappedUnit.
        /// </exception>
        /// <exception cref="FailException">
        /// Thrown when any other error other than those specified above occurs while locking the
        /// MemoryMappedUnit.
        /// </exception>
        /// <seealso cref="Lock"/>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1021:AvoidOutParameters",
            Justification = "This is by design."
        )]
        IntPtr DicomLock(out int msgLen, out int pixelDataOffset);

        /// <summary>
        /// Gets the number of locks at this moment on the current MemoryMappedUnit.
        /// </summary>
        int LockCount { get; }

        /// <summary>
        /// Gets the Identifier for the MMU.
        /// </summary>
        long Identifier { get; }

        /// <summary>
        /// Makes the MMF writable
        /// </summary>
        void MakeWritable();

        /// <summary>
        /// Gets the reference at this moment for the current memory mapped unit.
        /// </summary>
        /// <returns>Reference count for the current memory mapped unit, 0 if none.</returns>
        /// <exception cref="FailException">
        /// Thrown when any error occurs while getting the reference count.
        /// </exception>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while getting the 
        /// reference count for the current MemoryMappedUnit.
        /// </exception>
        int ReferenceCount { get; }

        /// <summary>
        /// Gets a value indicating if the object is preloaded on server
        /// </summary>
        bool IsPreloaded { get; }

        /// <summary>
        /// Gets the unconverted DICOM object. 
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>
        /// Unconverted header is deserialized on client side always on the first call.
        /// On further calls, the cached instance is returned.
        /// </item>
        /// <item>
        /// Even in cases of prefetch, when asked for unconverted header, the DICOM file will be
        /// deseialized on the client side as server always gives converted header.
        /// </item>
        /// </list>
        /// </remarks>
        DicomObject UnconvertedDicomObject { get; }

        /// <summary>
        /// Releases all the resources held by this MemoryMappedUnit including those on server.
        /// This is similar to calling <see cref="IDisposable.Dispose"/> but also releasing
        /// the resources on the server.
        /// </summary>
        /// <exception cref="FailException">
        /// Thrown when any error occurs while releasing the resources.
        /// </exception>
        /// <exception cref="ServerNotAvailableException">
        /// Thrown when the communication with the server drops down while releasing the
        /// resources held by this MemoryMappedUnit.
        /// </exception>
        /// <remarks>
        /// Releasing of server resources depends on the reference count of the
        /// MemoryMappedUnit on the server. Only when the reference count is 0, the
        /// resources will be released on the server.
        /// </remarks>
        void ReleaseResources();

        /// <summary>
        /// Increases the load priority of the mmu.
        /// </summary>
        void IncreasePriority();
    }
}
