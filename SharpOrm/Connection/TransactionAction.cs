using System.Data.Common;

namespace SharpOrm.Connection
{
    public delegate void TransactionCall(ConnectionManager manager);
    public delegate T TransactionCall<T>(ConnectionManager manager);
}