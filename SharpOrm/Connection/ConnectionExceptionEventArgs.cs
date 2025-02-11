using System;

namespace SharpOrm.Connection
{
    public class ConnectionExceptionEventArgs : EventArgs
    {
        private Exception Exception { get; }

        public ConnectionExceptionEventArgs(Exception exception)
        {
            this.Exception = exception;
        }
    }
}
