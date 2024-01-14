namespace SharpOrm
{
    public enum TrashedItems
    {
        /// <summary>
        /// Ignore the values that have been marked for removal.
        /// </summary>
        Ignore,
        /// <summary>
        /// Includes the values that have been marked for removal.
        /// </summary>
        With,
        /// <summary>
        /// Returns only the values that have been marked for removal.
        /// </summary>
        Only
    }
}
