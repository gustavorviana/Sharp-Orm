﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace SharpOrm.Builder.DataTranslation
{
    /// <summary>
    /// Represents a table translator for database table operations.
    /// </summary>
    public class TableReader : TableReaderBase
    {
        private readonly Queue<ForeignInfo> foreignKeyToLoad = new Queue<ForeignInfo>();
        private readonly ConcurrentDictionary<ForeignTable, object> cachedValues = new ConcurrentDictionary<ForeignTable, object>();
        private readonly string[] foreignsTables = null;
        private readonly int maxDepth = 0;
        private int currentDepth = 0;

        /// <summary>
        /// Property to check whether foreign tables need to be found.
        /// </summary>
        public bool FindForeigns => this.foreignsTables != null;

        /// <summary>
        /// Property to determine if a foreign object should be created with id when no depth is specified.
        /// </summary>
        public bool CreateForeignIfNoDepth { get; set; }

        public TableReader(IQueryConfig config, string[] tables, int maxDepth) : base(config)
        {
            this.foreignsTables = tables;
            this.maxDepth = maxDepth;
        }

        public TableReader(IQueryConfig config) : base(config)
        {

        }

        public override void LoadForeignKeys()
        {
            while (this.FindForeigns && foreignKeyToLoad.Count > 0)
            {
                ForeignInfo info = foreignKeyToLoad.Dequeue();
                this.currentDepth = Math.Max(info.Depth, this.currentDepth);

                if (info.Depth > maxDepth)
                    continue;

                info.SetForeignValue(this.GetValueFor(info));
            }
        }

        /// <inheritdoc />
        protected override object ParseFromReader(Type typeToParse, DbDataReader reader, string prefix)
        {
            var table = GetTable(typeToParse);
            object obj = table.CreateInstance();

            foreach (var column in table.Columns)
            {
                string fullName = string.IsNullOrEmpty(prefix) ? column.Name : $"{prefix}_{column.Name}";
                if (IsValidColumn(column, reader, fullName))
                    LoadColumnValue(obj, column, reader, fullName);
            }

            return obj;
        }

        /// <summary>
        /// Checks if the column is valid and can be loaded from the database reader.
        /// </summary>
        /// <param name="column">The column to check.</param>
        /// <param name="reader">The database reader.</param>
        /// <param name="fullName">The full column name.</param>
        /// <returns><c>true</c> if the column is valid; otherwise, <c>false</c>.</returns>
        private bool IsValidColumn(ColumnInfo column, DbDataReader reader, string fullName)
        {
            if (column.IsForeignKey)
                return true;

            bool isNative = NativeSqlValueConversor.IsNative(column.Type);
            var name = column.AutoGenerated ? column.Name : fullName;
            var index = reader.GetIndexOf(name);
            if ((!isNative || index == -1) && !column.Required)
                return false;

            return true;
        }

        /// <summary>
        /// Loads the column value from the database reader into the object.
        /// </summary>
        /// <param name="owner">The object to load the column value into.</param>
        /// <param name="column">The column information.</param>
        /// <param name="reader">The database reader.</param>
        /// <param name="fullName">The full column name.</param>
        private void LoadColumnValue(object owner, ColumnInfo column, DbDataReader reader, string fullName)
        {
            if (NativeSqlValueConversor.IsNative(column.Type))
            {
                string name = column.AutoGenerated ? column.Name : fullName;
                int index = reader.GetIndexOf(name);
                if (index < 0)
                    throw new KeyNotFoundException($"Could not find column in the database with key {name}. Failed to load value for {column.DeclaringType.FullName}.{column.Name}.");

                column.Set(owner, ObjectLoader.LoadFromDatabase(reader[index], this.config));
                return;
            }

            if (column.IsForeignKey) this.EnqueueForeign(owner, reader.Get(column.ForeignKey), column);
            else column.SetRaw(owner, ParseFromReader(column.Type, reader, fullName));
        }

        private void EnqueueForeign(object owner, object fkValue, ColumnInfo column)
        {
            var info = new ForeignInfo(owner, fkValue, column, currentDepth + 1);
            if (!this.FindForeigns)
            {
                if (this.CreateForeignIfNoDepth && GetObjWithKey(info) is object value)
                    info.SetForeignValue(value);
                return;
            }

            if (this.CanFindForeign(info.TableName))
                foreignKeyToLoad.Enqueue(info);
        }

        private object GetObjWithKey(ForeignInfo info)
        {
            var fkTable = GetTable(info.ForeignColumn.Type);
            object value = fkTable.CreateInstance();
            fkTable.Columns.FirstOrDefault(c => c.Key)?.Set(value, info.ForeignKey);

            return value;
        }

        private object GetValueFor(ForeignInfo info)
        {
            return this.cachedValues.GetOrAdd(new ForeignTable(info), (table) =>
            {
                using (var query = this.CreateQuery(table.TableName))
                {
                    query.Where("id", table.KeyValue);
                    query.Limit = 1;

                    using (var reader = query.ExecuteReader())
                        return reader.Read() ? this.ParseFromReader(info.ForeignColumn.Type, reader, "") : null;
                }
            });
        }

        private bool CanFindForeign(string name)
        {
            if (this.foreignsTables == null)
                return false;

            if (this.foreignsTables.Length == 0)
                return true;

            return this.foreignsTables.Any(t => t.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            foreignKeyToLoad.Clear();
            cachedValues.Clear();
        }

        public override IEnumerable<T> GetEnumerable<T>(DbDataReader reader, CancellationToken token)
        {
            var table = GetTable(typeof(T));
            if (table == null || table.HasNonNative)
            {
                while (reader.Read())
                {
                    token.ThrowIfCancellationRequested();
                    yield return this.ParseFromReader<T>(reader);
                }

                yield break;
            }

            var tReader = new RowReader(this, table);
            while (reader.Read())
            {
                token.ThrowIfCancellationRequested();
                yield return (T)tReader.GetRow(reader);
            }
        }

        private class ForeignTable : IEquatable<ForeignTable>
        {
            public string TableName { get; }
            public object KeyValue { get; }

            public ForeignTable(ForeignInfo info)
            {
                this.TableName = info.TableName;
                this.KeyValue = info.ForeignKey;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as ForeignTable);
            }

            public bool Equals(ForeignTable other)
            {
                return !(other is null) &&
                       TableName == other.TableName &&
                       EqualityComparer<object>.Default.Equals(KeyValue, other.KeyValue);
            }

            public override int GetHashCode()
            {
                return this.TableName.GetHashCode() * this.KeyValue.GetHashCode();
            }

            public static bool operator ==(ForeignTable left, ForeignTable right)
            {
                return EqualityComparer<ForeignTable>.Default.Equals(left, right);
            }

            public static bool operator !=(ForeignTable left, ForeignTable right)
            {
                return !(left == right);
            }
        }

        private class ForeignInfo
        {
            public object Owner { get; }
            public object ForeignKey { get; }
            public ColumnInfo ForeignColumn { get; }
            public int Depth { get; }
            public string TableName => TableInfo.GetNameOf(ForeignColumn.Type);

            public ForeignInfo(object owner, object foreignKey, ColumnInfo foreignColumn, int depth)
            {
                this.Owner = owner;
                this.ForeignKey = foreignKey;
                this.ForeignColumn = foreignColumn;
                this.Depth = depth;
            }

            public void SetForeignValue(object value)
            {
                this.ForeignColumn.SetRaw(this.Owner, value);
            }
        }

        private class RowReader
        {
            private readonly Dictionary<int, ColumnInfo> colsMap = new Dictionary<int, ColumnInfo>();
            private readonly Dictionary<int, ColumnInfo> fkMap = new Dictionary<int, ColumnInfo>();

            private readonly TableReader reader;
            private readonly TableInfo table;
            private bool hasFirstRead = false;
            public bool IsComplex { get; }

            public RowReader(TableReader reader, TableInfo table)
            {
                this.reader = reader;
                this.table = table;
            }

            public object GetRow(DbDataReader reader)
            {
                var owner = this.table.CreateInstance();
                if (hasFirstRead)
                {
                    foreach (var kv in colsMap)
                        kv.Value.Set(owner, reader[kv.Key]);

                    this.LoadFkObjs(owner, reader);
                    return owner;
                }

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i).ToLower();
                    if (table.Columns.FirstOrDefault(c => name == c.Name.ToLower()) is ColumnInfo ci)
                    {
                        ci.Set(owner, reader[i]);
                        colsMap[i] = ci;
                    }
                }

                this.LoadFkObjs(owner, reader);

                this.hasFirstRead = true;
                return owner;
            }

            private void LoadFkObjs(object owner, DbDataReader reader)
            {
                if (!this.table.HasFk)
                    return;

                if (hasFirstRead)
                {
                    foreach (var kv in fkMap)
                        this.reader.EnqueueForeign(owner, reader[kv.Key], kv.Value);

                    return;
                }

                foreach (var col in table.Columns.Where(x => x.IsForeignKey))
                {
                    int index = reader.GetIndexOf(col.ForeignKey);
                    if (index == -1)
                        continue;

                    this.fkMap[index] = col;
                    this.reader.EnqueueForeign(owner, reader[index], col);
                }
            }
        }
    }
}
