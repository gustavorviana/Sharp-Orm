namespace SharpOrm.Interceptors
{
    /// <summary>
    /// Represents the type of operation requested for an entity entry.
    /// </summary>
    public enum EntryState
    {
        /// <summary>
        /// No operation should be performed on the entity.
        /// </summary>
        None,

        /// <summary>
        /// The entity is being added.
        /// </summary>
        Add,

        /// <summary>
        /// The entity is being updated.
        /// </summary>
        Update,

        /// <summary>
        /// The entity is being removed.
        /// </summary>
        Remove
    }
}
