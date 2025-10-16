using System;

namespace SharpOrm.Errors
{
    /// <summary>
    /// Exception thrown when a column access operation fails.
    /// This includes scenarios such as:
    /// - Column index is out of range
    /// - Column name does not exist
    /// - Column type mismatch during type conversion
    /// </summary>
    public class DataReaderColumnException : DataReaderException
    {
        /// <summary>
        /// Gets the column name that caused the exception, if applicable.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Gets the column index that caused the exception, if applicable.
        /// </summary>
        public int? ColumnIndex { get; }

        /// <summary>
        /// Initializes a new instance of the DataReaderColumnException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DataReaderColumnException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the DataReaderColumnException class with a specified error message
        /// and column name.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="columnName">The name of the column that caused the exception.</param>
        public DataReaderColumnException(string message, string columnName) : base(message)
        {
            ColumnName = columnName;
        }

        /// <summary>
        /// Initializes a new instance of the DataReaderColumnException class with a specified error message
        /// and column index.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="columnIndex">The index of the column that caused the exception.</param>
        public DataReaderColumnException(string message, int columnIndex) : base(message)
        {
            ColumnIndex = columnIndex;
        }

        /// <summary>
        /// Initializes a new instance of the DataReaderColumnException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public DataReaderColumnException(string message, Exception inner) : base(message, inner) { }
    }
}
