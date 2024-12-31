using System;

namespace SharpOrm
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SoftDeleteAttribute : Attribute
    {
        public string ColumnName { get; }
        public string DateColumnName { get; set; }

        public SoftDeleteAttribute(string columnName = "deleted")
        {
            this.ColumnName = string.IsNullOrEmpty(columnName) ? "deleted" : columnName;
        }
    }
}
