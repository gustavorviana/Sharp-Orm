namespace SharpOrm
{
    public enum TrashVisibility
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
