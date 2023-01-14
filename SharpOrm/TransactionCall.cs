using System.Data.Common;

namespace SharpOrm
{
    public delegate void TransactionCall(DbTransaction transaction);
    public delegate T TransactionCall<T>(DbTransaction transaction);
}