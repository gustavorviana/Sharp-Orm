using System;

namespace SharpOrm.Errors
{
    [Serializable]
    /// <summary>
    /// Represents an exception that occurs during database connection operations.
    /// </summary>
    public class DbConnectionException : DatabaseException
    {
        /// <summary>
        /// Initializes a new instance of the DbConnectionException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DbConnectionException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the DbConnectionException class with a specified inner exception.
        /// </summary>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public DbConnectionException(Exception inner) : base(inner.Message, inner) { }

        /// <summary>
        /// Initializes a new instance of the DbConnectionException class with a specified error message
        /// and a reference to the inner exception that caused the current exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public DbConnectionException(string message, Exception inner) : base(message, inner) { }
    }

}
