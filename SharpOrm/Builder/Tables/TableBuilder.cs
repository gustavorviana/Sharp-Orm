using SharpOrm.Builder.Grammars.Table.Constraints;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.Builder.Tables
{
    public class TableBuilder<T> : TableBuilder, ITableBuilder<T>
    {
        private readonly TableInfo _tableInfo;

        public TableBuilder(TranslationRegistry registry)
        {
            _tableInfo = registry.GetTable(typeof(T));

            foreach (var column in _tableInfo.Columns)
                AddColumn(column);
        }

        public ITableBuilder<T> SetBasedTable(string table, Expression<ColumnExpression<T>> columnExpression)
        {
            var query = Query<T>.ReadOnly(table);
            if (columnExpression != null)
                query.Select(columnExpression);

            base.SetBasedQuery(query);
            return this;
        }

        public IColumnBuilder AddColumn(Expression<ColumnExpression<T>> expression)
        {
            return base.AddColumn(_tableInfo.Columns.Find(expression));
        }

        public ITableBuilder<T> HasKey(Expression<ColumnExpression<T>> expression)
        {
            base.HasKey(GetColumnNames(expression));
            return this;
        }

        public ITableBuilder<T> HasUnique(Expression<ColumnExpression<T>> expression, string constraintName = null)
        {
            base.HasUnique(GetColumnNames(expression), constraintName);
            return this;
        }

        public ITableBuilder<T> Ignore(Expression<ColumnExpression<T>> expression)
        {
            foreach (var name in GetColumnNames(expression))
                base.Ignore(name);

            return this;
        }

        public IIndexBuilder HasIndex(Expression<ColumnExpression<T>> expression)
        {
            return base.HasIndex(GetColumnNames(expression));
        }

        private string[] GetColumnNames(Expression<ColumnExpression<T>> expression)
        {
            return _tableInfo.Columns.FindAll(expression).Select(x => x.Name).ToArray();
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

        public ITableBuilder SetBasedTable(string table, params string[] columns)
        {
            var query = Query.ReadOnly(table);
            if (columns.Length > 0)
                query.Select(columns);

            return SetBasedQuery(query);
        }

        public ITableBuilder SetBasedQuery(QueryBase query)
        {
            Metadata.Add(Metadatas.BasedQuery, query);
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

        public IColumnBuilder AddColumn(ColumnInfo columnInfo)
        {
            var column = AddColumn(columnInfo.Name, columnInfo.Type);

            if (!string.IsNullOrEmpty(columnInfo.TypeName))
                column.HasColumnType(columnInfo.TypeName);

            if (columnInfo.Translation is INullableSqlTranslation)
                column.IsOptional();

            return column;
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

            type = TranslationUtils.GetValidTypeFor(type, out bool isNullable);
            var column = new ColumnBuilder(this, new System.Data.DataColumn(columnName, type));

            if (isNullable)
                column.IsOptional();

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
