using System;

namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// Specifies the mode for reading data.
    /// </summary>
    public enum ReadMode
    {
        /// <summary>
        /// No data is read.
        /// </summary>
        None = 0,
        /// <summary>
        /// Only valid data is read.
        /// </summary>
        ValidOnly = 1,
        /// <summary>
        /// All data is read.
        /// </summary>
        All = 2
    }
}
