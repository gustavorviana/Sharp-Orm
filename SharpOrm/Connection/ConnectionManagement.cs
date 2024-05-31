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
        /// The connection should be closed when <see cref="ConnectionManager"/> beginning resource release.
        /// </summary>
        CloseOnManagerDispose,
        /// <summary>
        /// The connection will be closed and its resources released (calling .Dispose() on the connection. Typically used when the connection was created without the ConnectionCreator.).
        /// </summary>
        DisposeOnManagerDispose,
        /// <summary>
        /// The connection should remain open (it is the developer's responsibility to manually close it).
        /// </summary>
        LeaveOpen
    }
}
