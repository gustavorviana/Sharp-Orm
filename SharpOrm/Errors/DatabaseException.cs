using System;

namespace SharpOrm.Errors
{
    /// <summary>
    /// This class defines a custom exception type, DatabaseException, which inherits from the base Exception class.
    /// </summary>
    public class DatabaseException : Exception
    {
        /// <summary>
        /// Protected internal parameterless constructor to restrict instantiation from outside assemblies.
        /// </summary>
        protected internal DatabaseException() { }

        /// <summary>
        /// Constructor that accepts a message string to describe the exception.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        public DatabaseException(string message) : base(message) { }

        /// <summary>
        /// Constructor that accepts both a message string and an inner exception, useful for chaining exceptions.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        /// <param name="inner">The exception that caused this exception to be thrown.</param>
        public DatabaseException(string message, Exception inner) : base(message, inner) { }
    }
}
