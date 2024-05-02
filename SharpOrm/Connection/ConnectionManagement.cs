namespace SharpOrm.Connection
{
    /// <summary>
    /// Types of connection management.
    /// </summary>
    public enum ConnectionManagement
    {
        /// <summary>
        /// The connection should be closed after each operation execution.
        /// </summary>
        CloseOnEndOperation,
        /// <summary>
        /// The connection should be closed when beginning resource release.
        /// </summary>
        CloseOnDispose,
        /// <summary>
        /// The connection should remain open (it is the developer's responsibility to manually close it).
        /// </summary>
        LeaveOpen
    }
}
