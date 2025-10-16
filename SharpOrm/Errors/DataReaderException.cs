using System;

namespace SharpOrm.Errors
{
    /// <summary>
    /// Base exception class for errors that occur during data reader operations.
    /// This exception is thrown when an error occurs while reading data from a RowReader.
    /// </summary>
    public class DataReaderException : Exception
    {
        /// <summary>
        /// Protected internal parameterless constructor to restrict instantiation from outside assemblies.
        /// </summary>
        protected internal DataReaderException() { }

        /// <summary>
        /// Initializes a new instance of the DataReaderException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DataReaderException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the DataReaderException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public DataReaderException(string message, Exception inner) : base(message, inner) { }
    }
}
