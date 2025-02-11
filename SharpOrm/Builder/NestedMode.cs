namespace SharpOrm.Builder
{
    /// <summary>
    /// Defines the nesting modes used in query building.
    /// </summary>
    public enum NestedMode
    {
        /// <summary>
        /// Applies nesting only to properties with <see cref="MapNestedAttribute"/>.
        /// </summary>
        Attribute,

        /// <summary>
        /// Applies nesting to all available elements.
        /// </summary>
        All
    }
}
