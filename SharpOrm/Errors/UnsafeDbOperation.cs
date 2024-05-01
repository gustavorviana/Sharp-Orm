using System;

namespace SharpOrm.Errors
{

    [Serializable]
    public class UnsafeDbOperation : DatabaseException
    {
        public UnsafeDbOperation() { }
        public UnsafeDbOperation(string message) : base(message) { }
        public UnsafeDbOperation(string message, Exception inner) : base(message, inner) { }
    }
}
