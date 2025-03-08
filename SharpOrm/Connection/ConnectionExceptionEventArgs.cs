using System;

namespace SharpOrm.Connection
{
    public class ConnectionExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; }

        public ConnectionExceptionEventArgs(Exception exception)
        {
            this.Exception = exception;
        }
    }
}
