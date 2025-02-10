using System;
using System.Collections.Generic;
using System.Text;

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
