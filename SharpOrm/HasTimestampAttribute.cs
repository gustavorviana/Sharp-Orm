using System;

namespace SharpOrm
{
    /// <summary>
    /// Attribute to indicate that a class or struct has timestamp columns.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HasTimestampAttribute : Attribute
    {
        /// <summary>
        /// Column name for the creation timestamp.
        /// </summary>
        /// <remarks>
        /// If you do not want the creation column, just set it to null.
        /// </remarks>
        public string CreatedAtColumn { get; set; } = "CreatedAt";

        /// <summary>
        /// Column name for the update timestamp.
        /// </summary>
        /// <remarks>
        /// If you do not want the update column, just set it to null.
        /// </remarks>
        public string UpdatedAtColumn { get; set; } = "UpdatedAt";
    }
}
