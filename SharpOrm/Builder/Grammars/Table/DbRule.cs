using System;

namespace SharpOrm.Builder.Grammars.Table
{
    /// <summary>
    /// Specifies the action to take on related rows when a row with a foreign key is updated or deleted.
    /// </summary>
    public enum DbRule
    {
        /// <summary>
        /// No action taken on related rows.
        /// </summary>
        None,
        /// <summary>
        /// Delete or update related rows. This is the default.
        /// </summary>
        Cascade,
        /// <summary>
        /// Set values in related rows to <see cref="DBNull"/>.
        /// </summary>
        SetNull,
        /// <summary>
        /// Set values in related rows to the value contained in the <see cref="System.Data.DataColumn.DefaultValue"/> property.
        /// </summary>
        SetDefault
    }
}
