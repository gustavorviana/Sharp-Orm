namespace SharpOrm.Builder
{
    public interface IWithTableInfo
    {
        /// <summary>
        /// Gets the table information for the entity type.
        /// </summary>
        TableInfo TableInfo { get; }
    }
}
