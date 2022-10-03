using System;
using System.Runtime.Serialization;

namespace SharpOrm.Errors
{
    [Serializable]
    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message)
        {

        }

        public DatabaseException(string message, Exception innerException) : base(message, innerException)
        {

        }

        protected DatabaseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
