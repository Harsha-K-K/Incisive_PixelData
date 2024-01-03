using System;
using System.Runtime.Serialization;

namespace PixelDataImplementation
{
    /// <summary>
    /// The exception that is thrown when Memory-Manager comes across
    /// a file which is truncated
    /// </summary>
    [Serializable]
    public class FileTruncatedException : Exception
    {

        /// <summary>
        /// Creates a default instance of the FileTruncatedException class
        /// </summary>
        [Obsolete("Do not use empty constructor, use this exception with a meaningful description")]
        public FileTruncatedException()
        {
        }

        /// <summary>
        /// Creates an instance of the FileTruncatedException class with given error message.
        /// </summary>
        /// <param name="errorDescription">Short and clear description of the error condition.
        /// </param>
        public FileTruncatedException(string errorDescription) : base(errorDescription)
        {
        }

        /// <summary>
        /// Creates an instance with given error description and wraps a lower level device specific
        /// exception that needs to be converted to platform defined exception.
        /// </summary>
        /// <param name="errorDescription">Short, clear and meaningful description of the observed 
        /// device specific error condition.
        /// </param>
        /// <param name="innerException">lower level exception that needs to be wrapped / converted
        /// to platform defined exception.</param>
        public FileTruncatedException(string errorDescription, Exception innerException)
            : base(errorDescription, innerException)
        {
        }


        /// <summary>                
        /// Support for de-serialization of the exception when thrown across a communication (WCF,
        /// .Net remote, etc.,) boundaries.
        /// </summary>
        /// <param name="info">The serialized object information from which new exception instance
        /// has to be created.
        /// </param>
        /// <param name="context">Contextual information about the source or destination of this
        /// platform exception instance.
        /// </param>
        protected FileTruncatedException(
            SerializationInfo info,
            StreamingContext context
        ) : base(info, context)
        {
        }
    }
}