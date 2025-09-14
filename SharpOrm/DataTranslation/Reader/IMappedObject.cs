using System;
using System.Data;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// **Obsolete:** This interface is deprecated and will be replaced by <see cref="BaseRecordReader"/> in version 4.0.
    /// Represents an object that can be mapped from an <see cref="IDataRecord"/>.
    /// </summary>
    [Obsolete("IMappedObject is deprecated and will be removed in version 4.0. Use BaseRecordReader instead.")]
    public interface IMappedObject
    {
        /// <summary>
        /// Reads data from the database record and maps it to an object.
        /// </summary>
        /// <param name="record">The database data record.</param>
        /// <returns>The mapped object.</returns>
        object Read(IDataRecord record);
    }
}
