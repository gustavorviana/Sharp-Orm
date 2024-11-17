namespace SharpOrm
{
    public enum TrashVisibility
    {
        /// <summary>
        /// Ignore the values that have been marked as removed.
        /// </summary>
        Ignore,
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
