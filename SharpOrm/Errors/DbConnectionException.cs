using System;

namespace SharpOrm.Errors
{
    [Serializable]
    public class DbConnectionException : DatabaseException
    {
        public DbConnectionException(string message) : base(message) { }
        public DbConnectionException(Exception inner) : base(inner.Message, inner) { }
        public DbConnectionException(string message, Exception inner) : base(message, inner) { }
    }
}
