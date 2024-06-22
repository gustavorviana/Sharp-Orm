using System;

namespace SharpOrm.Errors
{

    [Serializable]
    /// <summary>
    /// Represents an exception that occurs when performing unsafe operations on the database.
    /// </summary>
    public class UnsafeDbOperation : DatabaseException
    {
        /// <summary>
        /// Initializes a new instance of the UnsafeDbOperation class.
        /// </summary>
        public UnsafeDbOperation() { }

        /// <summary>
        /// Initializes a new instance of the UnsafeDbOperation class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnsafeDbOperation(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the UnsafeDbOperation class with a specified error message and a reference to the inner exception that caused the current exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public UnsafeDbOperation(string message, Exception inner) : base(message, inner) { }
    }
}
