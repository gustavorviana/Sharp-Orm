using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SoftDeleteAttribute : Attribute
    {
        public string ColumnName { get; }

        public SoftDeleteAttribute(string columnName)
        {
            this.ColumnName = string.IsNullOrEmpty(columnName) ? "deleted_at" : columnName;
        }
    }
}
