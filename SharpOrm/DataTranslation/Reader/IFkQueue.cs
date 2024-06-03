using SharpOrm.Builder;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Interface representing a foreign key queue.
    /// </summary>
    public interface IFkQueue
    {
        /// <summary>
        /// Enqueues a foreign key value.
        /// </summary>
        /// <param name="owner">The owner object of the foreign key.</param>
        /// <param name="fkValue">The foreign key value.</param>
        /// <param name="column">The column information associated with the foreign key.</param>
        void EnqueueForeign(object owner, object fkValue, ColumnInfo column);
    }
}
