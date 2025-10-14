namespace SharpOrm.Builder.Tables.Loaders
{
    /// <summary>
    /// Interface for objects that can load column information.
    /// </summary>
    internal interface IColumnLoader
    {
        /// <summary>
        /// Loads column information based on the implementation strategy.
        /// </summary>
        /// <returns>An enumerable of column information.</returns>
        ColumnCollection LoadColumns();
    }
}
