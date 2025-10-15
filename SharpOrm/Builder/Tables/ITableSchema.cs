using SharpOrm.Builder.Grammars.Table.Constraints;
using SharpOrm.Builder.Tables;
using System;
using System.Collections.Generic;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents an immutable database table schema definition containing columns, constraints, indexes, and metadata.
    /// </summary>
    public interface ITableSchema : ICloneable
    {
        /// <summary>
        /// Gets the collection of constraints defined for this table (primary keys, foreign keys, unique constraints, check constraints).
        /// </summary>
        IReadOnlyList<Constraint> Constraints { get; }

        /// <summary>
        /// Gets the collection of indexes defined for this table.
        /// </summary>
        IReadOnlyList<IndexDefinition> Indexes { get; }

        /// <summary>
        /// Gets the collection of columns defined for this table.
        /// </summary>
        IReadOnlyList<System.Data.DataColumn> Columns { get; }

        /// <summary>
        /// Gets the metadata associated with this table schema.
        /// </summary>
        IMetadata Metadata { get; }

        /// <summary>
        /// Gets the name of the table.
        /// For temporary tables, this includes a GUID prefix in the format: {guid}_{tableName}.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this table is temporary.
        /// Temporary tables are automatically prefixed with a GUID to ensure uniqueness.
        /// </summary>
        bool Temporary { get; }

        /// <summary>
        /// Creates a shallow copy of the current table schema.
        /// </summary>
        /// <returns>A new <see cref="ITableSchema"/> instance with the same properties.</returns>
        new ITableSchema Clone();
    }
}