using System;

namespace SharpOrm.Builder.Expressions
{
    [Flags]
    internal enum ExpressionConfig
    {
        None = 0,
        All = New | SubMembers | Method,
        New = 1,
        SubMembers = 2,
        Method = 4
    }
}
