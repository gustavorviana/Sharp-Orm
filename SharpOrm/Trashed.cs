namespace SharpOrm
{
    /// <summary>
    /// Enum representing the state of trashed items.
    /// </summary>
    public enum Trashed
    {
        /// <summary>
        /// Except the values that have been marked as removed.
        /// </summary>
        Except,
        /// <summary>
        /// Includes the values that have been marked as removed.
        /// </summary>
        With,
        /// <summary>
        /// Returns only the values that have been marked as removed.
        /// </summary>
        Only
    }
}
