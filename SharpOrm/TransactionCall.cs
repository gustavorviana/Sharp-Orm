using System;
using System.Data.Common;

namespace SharpOrm
{
    [Obsolete("This function is deprecated, use SharpOrm.Connection.TransactionCall. It will be removed in version 3.0.")]
    public delegate void TransactionCall(DbTransaction transaction);
    [Obsolete("This function is deprecated, use SharpOrm.Connection.TransactionCall<T>. It will be removed in version 3.0.")]
    public delegate T TransactionCall<T>(DbTransaction transaction);
}