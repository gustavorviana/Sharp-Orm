using SharpOrm.Builder.Grammars.Table.Constraints;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder.Tables
{
    public class TableBuilder<T> : TableBuilder
    {
        public TableBuilder()
        {
            Metadata.Add(Metadatas.BasedQuery, null);
        }
    }

    public class TableBuilder : ITableBuilder
    {
        private string _name;
        private TableSchema _schema;
        private readonly Metadata _metadata = new Metadata();
        private readonly List<Constraint> _constraints = new List<Constraint>();
        private readonly HashSet<string> _ignoredColumns = new HashSet<string>();
        private readonly List<IndexDefinition> _indexes = new List<IndexDefinition>();
        private readonly Dictionary<string, ColumnBuilder> _columns = new Dictionary<string, ColumnBuilder>();

        protected bool Builded => _schema != null;
        public IMetadata Metadata => _metadata;
        protected virtual bool Temporary { get; }

        public ITableBuilder SetName(string tableName)
        {
            _name = tableName;
            return this;
        }

        public ITableBuilder HasCheck(string expression, string constraintName)
        {
            ThrowAlreadyBuilded();

            _constraints.Add(new CheckConstraint(_name, expression, constraintName));
            return this;
        }

        public ITableBuilder HasForeignKey(string columnName, string referencedTable, string referencedColumn = "Id", string constraintName = null)
        {
            ThrowAlreadyBuilded();

            _constraints.Add(new ForeignKeyConstraint(_name, columnName, referencedTable, referencedColumn, constraintName));
            return this;
        }

        public ITableBuilder HasKey(params string[] columnNames)
        {
            ThrowAlreadyBuilded();

            foreach (var columnName in columnNames)
            {
                if (!_columns.TryGetValue(columnName, out var column))
                    throw new InvalidOperationException($"The column '{columnName}' does not exist and cannot be set as a primary key.");

                column.IsRequired();
            }

            _constraints.Add(new PrimaryKeyConstraint(_name, columnNames));
            return this;
        }

        public ITableBuilder HasUnique(string columnName, string constraintName = null)
        {
            return HasUnique(new string[] { columnName }, constraintName);
        }

        public ITableBuilder HasUnique(string[] columnNames, string constraintName = null)
        {
            ThrowAlreadyBuilded();

            _constraints.Add(new UniqueConstraint(_name, columnNames, constraintName));
            return this;
        }

        public ITableBuilder AddConstraint(Constraint constraint)
        {
            ThrowAlreadyBuilded();

            _constraints.Add(constraint);
            return this;
        }

        public IColumnBuilder AddColumn(string columnName, Type type)
        {
            ThrowAlreadyBuilded();

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (Metadata.HasKey(Metadatas.BasedQuery))
                throw new InvalidOperationException("Cannot add a column when a base query has been defined for this table.");

            if (_ignoredColumns.Contains(columnName))
                throw new InvalidOperationException($"The column '{columnName}' has been ignored and cannot be added.");

            if (_columns.TryGetValue(columnName, out ColumnBuilder builder))
                return builder.HasType(type);

            var column = new ColumnBuilder(this, new System.Data.DataColumn(columnName, type));
            _columns.Add(columnName, column);
            return column;
        }

        public ITableBuilder Ignore(string columnName)
        {
            ThrowAlreadyBuilded();

            if (!_ignoredColumns.Add(columnName))
                return this;

            _columns.Remove(columnName);
            return this;
        }

        public IIndexBuilder HasIndex(string columnName)
        {
            return HasIndex(new string[] { columnName });
        }

        public IIndexBuilder HasIndex(params string[] columnNames)
        {
            ThrowAlreadyBuilded();

            var builder = new IndexBuilder(columnNames);
            _indexes.Add(builder.Definition);
            return builder;
        }

        private void ThrowAlreadyBuilded()
        {
            if (_schema != null)
                throw new InvalidOperationException("The table schema has already been built and cannot be modified.");
        }

        public virtual ITableSchema GetSchema()
        {
            if (_schema != null)
                return _schema;

            var tableName = GetTableName();

            return _schema = new TableSchema(tableName,
                Temporary,
                _constraints.AsReadOnly(),
                _indexes.AsReadOnly(),
                _columns.Select(x => x.Value._column).ToArray(),
                _metadata.MakeReadonly()
            );
        }

        protected virtual string GetTableName()
        {
            return _name;
        }

        private class TableSchema : ITableSchema
        {
            public IReadOnlyList<Constraint> Constraints { get; }

            public IReadOnlyList<IndexDefinition> Indexes { get; }

            public IReadOnlyList<System.Data.DataColumn> Columns { get; }

            public string Name { get; }

            public bool Temporary { get; }

            public IMetadata Metadata { get; }

            public TableSchema(string name,
                bool temporary,
                IReadOnlyList<Constraint> constraints,
                IReadOnlyList<IndexDefinition> indexes,
                IReadOnlyList<System.Data.DataColumn> columns,
                IMetadata metadata)
            {
                Name = name;
                Temporary = temporary;
                Constraints = constraints;
                Indexes = indexes;
                Columns = columns;
                Metadata = metadata;
            }

            public ITableSchema Clone() => new TableSchema(Name, Temporary, Constraints, Indexes, Columns, Metadata);
            object ICloneable.Clone() => Clone();
        }
    }
}
