using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Fb
{
    internal static partial class FbMessages
    {
        internal static partial class Grammar
        {
            public const string DeleteIncludingJoinsNotSupported = "Delete operations on multiple tables with JOINs are not supported in Firebird. Please execute separate DELETE statements for each table.";
            public const string OffsetRequiresLimit = "To use OFFSET in this operation, it is necessary to use LIMIT as well.";
        }
    }
}
