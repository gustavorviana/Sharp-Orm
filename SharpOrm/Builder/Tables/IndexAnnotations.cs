using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Tables
{
    public static class IndexAnnotations
    {
        // SQL Server specific
        public const string FillFactor = "FillFactor";
        public const string IncludedColumns = "Include";
        public const string Filter = "Filter";
        public const string Online = "Online";
        public const string Filegroup = "Filegroup";
        public const string IgnoreDuplicateKeys = "IgnoreDuplicateKeys";

        // MySQL specific
        public const string IndexType = "IndexType";
        public const string KeyBlockSize = "KeyBlockSize";

        // PostgreSQL specific
        public const string IndexMethod = ":Method";
        public const string IndexWith = ":With";

        // Generic
        public const string Comment = "Comment";
        public const string Description = "Description";
    }

}
