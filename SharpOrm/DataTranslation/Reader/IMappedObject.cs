using System.Data;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Interface representing an object that can be mapped from a database reader.
    /// </summary>
    public interface IMappedObject
    {
        /// <summary>
        /// Reads data from the database reader and maps it to an object.
        /// </summary>
        /// <param name="reader">The database data reader.</param>
        /// <returns>The mapped object.</returns>
        object Read(IDataReader reader);
    }
}
