using System.Data;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Interface representing an object that can be mapped from a <see cref="IDataRecord"/>.
    /// </summary>
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
