using System;
using System.Runtime.Serialization;

namespace SharpOrm.Errors
{

    [Serializable]
    public class UnsafeDbOperation : DatabaseException
    {
        public UnsafeDbOperation() { }
        public UnsafeDbOperation(string message) : base(message) { }
        public UnsafeDbOperation(string message, Exception inner) : base(message, inner) { }
        protected UnsafeDbOperation(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
