﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    /// <summary>
    /// Represents a table translator for database table operations.
    /// </summary>
    public class TableReader : TableReaderBase
    {
        public static readonly TableReader Default = new TableReader();
        private readonly Queue<ForeignInfo> foreignKeyToLoad = new Queue<ForeignInfo>();
        private readonly ConcurrentDictionary<ForeignTable, object> cachedValues = new ConcurrentDictionary<ForeignTable, object>();
        private readonly string[] foreignsTables = null;
        private readonly int maxDept = 0;
        private int currentDepth = 0;
        public bool FindForeigns => this.foreignsTables != null;

        public TableReader(string[] tables, int maxDepth)
        {
            this.foreignsTables = tables;
            this.maxDept = maxDepth;
        }

        public TableReader()
        {

        }

        public void LoadForeignKeys()
        {
            while (this.FindForeigns && foreignKeyToLoad.Count > 0)
            {
                ForeignInfo info = foreignKeyToLoad.Dequeue();
                this.currentDepth = Math.Max(info.Depth, this.currentDepth);

                if (info.Depth > maxDept)
                    continue;

                this.cachedValues.TryAdd(new ForeignTable(info), info.Owner);
                info.SetForeignValue(this.GetValueFor(info));
            }
        }

        /// <inheritdoc />
        protected override object ParseFromReader(Type typeToParse, DbDataReader reader, string prefix)
        {
            var table = GetTable(typeToParse);
            object obj = Activator.CreateInstance(typeToParse);

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
            if (!string.IsNullOrEmpty(column.ForeignKey))
                return true;

            bool isNative = NativeSqlValueConversor.IsNative(column.Type);
            if ((!isNative || reader.GetIndexOf(fullName) == -1) && !column.Required)
                return false;

            return true;
        }

        /// <summary>
        /// Loads the column value from the database reader into the object.
        /// </summary>
        /// <param name="obj">The object to load the column value into.</param>
        /// <param name="column">The column information.</param>
        /// <param name="reader">The database reader.</param>
        /// <param name="fullName">The full column name.</param>
        private void LoadColumnValue(object obj, ColumnInfo column, DbDataReader reader, string fullName)
        {
            if (IsNativeColumn(column))
            {
                int index = reader.GetIndexOf(fullName);
                if (index < 0)
                    throw new KeyNotFoundException($"Could not find column in the database with key {fullName}. Failed to load value for {column.DeclaringType.FullName}.{column.Name}.");

                column.Set(obj, reader[index]);
                return;
            }

            if (!string.IsNullOrEmpty(column.ForeignKey)) this.EnqueueForeign(reader, column, obj);
            else column.SetRaw(obj, ParseFromReader(column.Type, reader, fullName));
        }

        private void EnqueueForeign(DbDataReader reader, ColumnInfo column, object obj)
        {
            if (!this.FindForeigns)
                return;

            object foreignKey = reader[column.ForeignKey];
            if (foreignKey == DBNull.Value)
                return;

            var fi = new ForeignInfo(obj, foreignKey, column, currentDepth + 1);
            if (this.CanFindForeign(fi.TableName))
                foreignKeyToLoad.Enqueue(fi);
        }

        /// <summary>
        /// Checks if the column is a native column.
        /// </summary>
        /// <param name="column">The column to check.</param>
        /// <returns><c>true</c> if the column is a native column; otherwise, <c>false</c>.</returns>
        private bool IsNativeColumn(ColumnInfo column)
        {
            return NativeSqlValueConversor.IsNative(column.Type) || column.Translation != null;
        }

        private object GetValueFor(ForeignInfo info)
        {
            return this.cachedValues.GetOrAdd(new ForeignTable(info), (table) =>
            {
                using (var query = new Query(table.TableName))
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

            name = name.ToLower();
            return this.foreignsTables.Any(t => t.ToLower() == name);
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
                return base.GetHashCode();
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
            public string TableName => GetTable(ForeignColumn.Type).Name;

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
    }
}