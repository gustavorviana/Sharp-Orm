using System;

namespace SharpOrm
{
    /// <summary>
    /// Attribute to mark a class for soft deletion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SoftDeleteAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the column that indicates soft deletion.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Gets or sets the name of the column that stores the deletion date.
        /// </summary>
        public string DateColumnName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftDeleteAttribute"/> class.
        /// </summary>
        /// <param name="columnName">The name of the column that indicates soft deletion. Default is "deleted".</param>
        public SoftDeleteAttribute(string columnName = "deleted")
        {
            this.ColumnName = string.IsNullOrEmpty(columnName) ? "deleted" : columnName;
        }
    }
}
