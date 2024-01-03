using Philips.Platform.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelDataImplementation
{
    /// <summary>
    /// Defines the necessary functions for supporting Application blobs
    /// </summary>
    internal interface IApplicationBlob
    {

        /// <summary>
        /// Check if the blob exists for the given storage key
        /// </summary>
        /// <param name="identifier">the identifier for the blob</param>
        /// <param name="blobName">blob name</param>
        /// <returns>true if the blob exists, false otherwise</returns>
        bool DoesBlobExist(Identifier identifier, string blobName);

        /// <summary>
        /// Check if the blob exists for the given blob name
        /// </summary>
        /// <param name="blobName">blob name</param>
        /// <returns>true if the blob exists, false otherwise</returns>
        bool DoesBlobExist(string blobName);

        /// <summary>
        /// Loads the given blob into a stream
        /// </summary>
        /// <param name="identifier">the identifier for the blob</param>
        /// <param name="blobName">blob name</param>
        /// <returns>Stream containing blob data</returns>
        Stream FetchBlob(Identifier identifier, string blobName);

        /// <summary>
        /// Loads the given blob into a stream
        /// </summary>
        /// <param name="blobName">blob name</param>
        /// <returns>Stream containing blob data</returns>
        Stream FetchBlob(string blobName);

        /// <summary>
        /// Loads all the blobs under the identifier.
        /// </summary>
        /// <param name="identifier">the identifier for the blob</param>        
        /// <returns>List of stream containing blob data under the identifier</returns>
        IList<Stream> FetchBlobs(Identifier identifier);

        /// <summary>
        /// Saves the given blob in the storage device
        /// </summary>
        /// <param name="identifier">the identifier for the blob</param>
        /// <param name="blobName">the name of the blob</param>
        /// <param name="blobStream">blob as a Stream</param>
        void StoreBlob(Identifier identifier, string blobName, Stream blobStream);

        /// <summary>
        /// Saves the given blob in the storage device
        /// </summary>
        /// <param name="blobName">the name of the blob</param>
        /// <param name="blobStream">blob as a Stream</param>
        void StoreBlob(string blobName, Stream blobStream);

        /// <summary>
        /// Deletes the blob for the given identifier
        /// </summary>
        /// <param name="identifier">the identifier for the blob</param>
        /// <param name="blobName">blob name</param>
        void DeleteBlob(Identifier identifier, string blobName);

        /// <summary>
        /// Deletes the blob for the given device and blob name
        /// </summary>
        /// <param name="blobName">blob name</param>
        void DeleteBlob(string blobName);

    }

}
