using SharpOrm.Builder.Grammars.Table.Constraints;
using SharpOrm.Builder.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using DataColumn = System.Data.DataColumn;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents the schema for creating a table in the database.
    /// </summary>
    [Obsolete("Use TableBuilder instead. This feature will be removed in version 4.0.")]
    public class TableSchema : ITableSchema
    {
        private readonly Metadata _metadata = new Metadata();
        private bool _builded;

        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the table should be temporary.
        /// </summary>
        public bool Temporary { get; set; }

        private readonly IReadOnlyList<DataColumn> _columns;

        IReadOnlyList<Grammars.Table.Constraints.Constraint> ITableSchema.Constraints { get; } = new List<Grammars.Table.Constraints.Constraint>();
        IReadOnlyList<IndexDefinition> ITableSchema.Indexes { get; } = new List<IndexDefinition>();
        IReadOnlyList<DataColumn> ITableSchema.Columns => _columns;
        IMetadata ITableSchema.Metadata => _metadata;

        /// <summary>
        /// Gets the query whose structure should be copied to the new table.
        /// </summary>
        public Query BasedQuery => _metadata.GetOrDefault<Query>(Metadatas.BasedQuery);

        /// <summary>
        /// Gets the collection of columns to be inserted into the new table.
        /// </summary>
        public readonly TableColumnCollection Columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableSchema"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <exception cref="ArgumentNullException">Thrown when the name is null or empty.</exception>
        public TableSchema(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Columns = new TableColumnCollection();
            _columns = Columns._columns;
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableSchema"/> class with the specified name and columns.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <param name="columns">The collection of columns.</param>
        /// <exception cref="ArgumentNullException">Thrown when the name is null or empty.</exception>
        public TableSchema(string name, TableColumnCollection columns)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            _columns = columns?._columns;
            if (_columns == null)
                _columns = DotnetUtils.EmptyArray<DataColumn>();

            Columns = columns;
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableSchema"/> class with the specified name and based query.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <param name="basedTableQuery">The query whose structure should be copied to the new table.</param>
        public TableSchema(string name, Query basedTableQuery)
        {
            Name = name;
            _metadata.Add(Metadatas.BasedQuery, basedTableQuery);
            _columns = DotnetUtils.EmptyArray<DataColumn>();
        }

        /// <summary>
        /// Creates a clone of the current table schema.
        /// </summary>
        /// <returns>A new instance of <see cref="TableSchema"/> that is a copy of the current instance.</returns>
        public ITableSchema Clone()
        {
            if (BasedQuery != null)
                return new TableSchema(Name, BasedQuery) { Temporary = Temporary };

            return new TableSchema(Name, Columns) { Temporary = Temporary };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        internal ITableSchema Build()
        {
            if (_builded)
            {
                var id = Guid.NewGuid().ToString("N");
                if (string.IsNullOrEmpty(Name)) Name = id;
                else Name = Guid.NewGuid().ToString("N") + "_" + Name;

                _builded = true;
            }

            var columns = Columns ?? new TableColumnCollection();

            var constraints = ((ITableSchema)this).Constraints as List<Grammars.Table.Constraints.Constraint>;
            constraints.Clear();

            var primaryKeys = columns.PrimaryKeys.Select(x => x.ColumnName).ToArray();
            var isPkAutoIncrement = columns.PrimaryKeys.Any(x => x.AutoIncrement);

            if (primaryKeys.Length > 0)
                constraints.Add(new PrimaryKeyConstraint(Name, primaryKeys) { AutoIncrement = isPkAutoIncrement });

            var uniques = columns.Where(x => x.Unique).Select(x => x.ColumnName).ToArray();
            if (uniques.Length > 0)
                constraints.Add(new UniqueConstraint(Name, uniques));

            _metadata.MakeReadonly();

            return this;
        }
    }
}
