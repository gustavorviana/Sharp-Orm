using SharpOrm.Builder.Tables;
using SharpOrm.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    public class DbTableValue<TValue> : IDbTableValue, IDisposable
    {
        private bool _disposed;
        private readonly DbTable _table;
        private readonly bool _canDispose;

        public DbName Table => _table.DbName;
        public string Column { get; private set; }

        public SqlExpression ToExpression(IReadonlyQueryInfo info)
        {
            var col = info.Config.ApplyNomenclature(Column);
            var table = info.Config.ApplyNomenclature(Table.Name);
            return new SqlExpression($"SELECT {col} FROM {table}");
        }

        public DbTableValue(DbTable table, string column)
            : this(table, column, false)
        {
        }

        private DbTableValue(DbTable table, string column, bool canDispose)
        {
            _canDispose = canDispose;
            _table = table;

            Column = column;
        }

        public static DbTableValue<TValue> FromValues(ConnectionManager manager, ICollection<TValue> values, string collation = null)
        {
            var column = typeof(TValue).Name;

            var builder = new TableBuilder("ValueTable", true);
            builder.AddColumn<TValue>(column).HasCollation(collation);

            var table = DbTable.Create(builder.GetSchema(), manager);

            using (var query = table.GetQuery())
                query.BulkInsert(values.Select(x => new Row(new Cell(column, x))));

            return new DbTableValue<TValue>(table, column, true);
        }

        public static DbTableValue<TValue> FromQuery(Query query, string column)
        {
            var name = query.Info.TableName.Name;
            if (name.Contains("_"))
            {
                var index = name.IndexOf("_");
                if (index > 0)
                    name = name.Substring(0, index);
            }

            var builder = new TableBuilder(name, true);
            builder.SetBasedQuery(query);

            var table = DbTable.Create(builder.GetSchema(), query.Manager);

            if (column.Contains("."))
                column = column.Substring(column.IndexOf('.') + 1);

            return new DbTableValue<TValue>(table, column, true);
        }

        public TValue[] ToArray()
        {
            using (var query = _table.GetQuery())
            {
                query.Select(Column);
                return query.ExecuteArrayScalar<TValue>();
            }
        }

        public SqlExpression CreateEqualsExpression(string column)
        {
            return CreateEqualsExpression((Column)column);
        }

        public SqlExpression CreateEqualsExpression(Column column)
        {
            using (var query = _table.GetQuery())
            {
                query.Where(string.Format("{0}.{1}", Table.Name, Column), column);
                return query.ToSqlExpression();
            }
        }

        public long Count()
        {
            using (var query = _table.GetQuery())
                return query.Count();
        }

        public IInsertIntoBuilder InsertInto(string table)
        {
            return new InsertIntoBuilder(this, table);
        }

        #region InsertInto Builder

        private sealed class InsertIntoBuilder : IInsertIntoBuilder
        {
            private readonly DbTableValue<TValue> _source;
            private readonly string _targetTable;
            private readonly List<InsertColumn> _columns = new List<InsertColumn>();

            internal InsertIntoBuilder(DbTableValue<TValue> source, string targetTable)
            {
                if (source == null) throw new ArgumentNullException("source");
                if (string.IsNullOrWhiteSpace(targetTable))
                    throw new ArgumentException("Target table is required.", "targetTable");

                _source = source;
                _targetTable = targetTable;
            }

            public IInsertIntoBuilder Add(string targetColumn, object value)
            {
                if (string.IsNullOrWhiteSpace(targetColumn))
                    throw new ArgumentException("Target column is required.", "targetColumn");

                _columns.Add(InsertColumn.Constant(targetColumn, value));
                return this;
            }

            public IInsertIntoBuilder Add(string targetColumn)
            {
                if (string.IsNullOrWhiteSpace(targetColumn))
                    throw new ArgumentException("Target column is required.", "targetColumn");

                _columns.Add(InsertColumn.FromSource(targetColumn, _source.Column));
                return this;
            }

            public IInsertIntoBuilder Add(string targetColumn, string sourceColumn)
            {
                if (string.IsNullOrWhiteSpace(targetColumn))
                    throw new ArgumentException("Target column is required.", "targetColumn");
                if (string.IsNullOrWhiteSpace(sourceColumn))
                    throw new ArgumentException("Source column is required.", "sourceColumn");

                _columns.Add(InsertColumn.FromSource(targetColumn, sourceColumn));
                return this;
            }

            public void Execute()
            {
                _source._table.Manager.ExecuteNonQuery(Build());
            }

            private SqlExpression Build()
            {
                if (_columns.Count == 0)
                    throw new InvalidOperationException("At least one Add(...) is required.");

                var targetCols = string.Join(", ", _columns.Select(c => QuoteColumn(c.TargetColumn)));
                var selectCols = string.Join(", ", _columns.Select(BuildSelectExpr));
                var fromTable = QuoteTable(_source.Table.Name);

                var sb = new StringBuilder();
                sb.Append("INSERT INTO ").Append(QuoteTable(_targetTable))
                  .Append(" (").Append(targetCols).Append(')')
                  .Append(" SELECT ").Append(selectCols)
                  .Append(" FROM ").Append(fromTable).Append(';');

                var parameters = new List<object>();
                foreach (var c in _columns)
                {
                    if (c.Kind == InsertColumnKind.Constant)
                        parameters.Add(c.Value);
                }

                return new SqlExpression(sb.ToString(), parameters);
            }

            private string BuildSelectExpr(InsertColumn c)
            {
                if (c.Kind == InsertColumnKind.SourceColumn)
                    return QuoteColumn(c.SourceColumn);
                if (c.Kind == InsertColumnKind.Constant)
                    return string.Format("? AS {0}", QuoteColumn(c.TargetColumn));

                throw new NotSupportedException("Insert column kind not supported: " + c.Kind);
            }

            private static string QuoteTable(string name)
            {
                var parts = SplitName(name);
                return string.Join(".", parts.Select(QuoteIdentifier));
            }

            private static string QuoteColumn(string name)
            {
                var parts = SplitName(name);
                return string.Join(".", parts.Select(QuoteIdentifier));
            }

            private static string[] SplitName(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return new string[0];

                var parts = new List<string>();
                var sb = new StringBuilder();
                var inBrackets = false;

                foreach (var ch in name)
                {
                    if (ch == '[') inBrackets = true;
                    if (ch == ']') inBrackets = false;

                    if (ch == '.' && !inBrackets)
                    {
                        parts.Add(sb.ToString().Trim());
                        sb.Length = 0;
                        continue;
                    }

                    sb.Append(ch);
                }

                if (sb.Length > 0)
                    parts.Add(sb.ToString().Trim());

                return parts.Where(p => p.Length > 0).ToArray();
            }

            private static string QuoteIdentifier(string ident)
            {
                ident = (ident ?? string.Empty).Trim();
                if (ident.Length == 0)
                    return ident;

                if (ident.StartsWith("[", StringComparison.Ordinal) && ident.EndsWith("]", StringComparison.Ordinal))
                    return ident;

                ident = ident.Replace("]", "]]");
                return "[" + ident + "]";
            }

            private struct InsertColumn
            {
                public InsertColumnKind Kind { get; private set; }
                public string TargetColumn { get; private set; }
                public string SourceColumn { get; private set; }
                public object Value { get; private set; }

                private InsertColumn(InsertColumnKind kind, string targetColumn, string sourceColumn, object value)
                    : this()
                {
                    Kind = kind;
                    TargetColumn = targetColumn;
                    SourceColumn = sourceColumn;
                    Value = value;
                }

                public static InsertColumn FromSource(string targetColumn, string sourceColumn)
                {
                    return new InsertColumn(InsertColumnKind.SourceColumn, targetColumn, sourceColumn, null);
                }

                public static InsertColumn Constant(string targetColumn, object value)
                {
                    return new InsertColumn(InsertColumnKind.Constant, targetColumn, null, value);
                }
            }

            private enum InsertColumnKind
            {
                SourceColumn = 0,
                Constant = 1
            }
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing && _canDispose)
                _table.Dispose();

            _disposed = true;
        }

        ~DbTableValue()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}