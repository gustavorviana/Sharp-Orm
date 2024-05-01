using System;

namespace SharpOrm.Errors
{
    public class DatabaseException : Exception
    {
        protected internal DatabaseException() { }

        public DatabaseException(string message) : base(message) { }

        public DatabaseException(string message, Exception inner) : base(message, inner) { }
    }
}
