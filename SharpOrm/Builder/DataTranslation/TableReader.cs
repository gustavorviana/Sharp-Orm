using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;

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
        private readonly bool findAllForeigns = false;
        public bool FindAllForeigns => this.findAllForeigns;

        public void LoadForeignKeys()
        {
            while (this.FindAllForeigns && foreignKeyToLoad.Count > 0)
            {
                ForeignInfo info = foreignKeyToLoad.Dequeue();
                this.cachedValues.TryAdd(new ForeignTable(info), info.Owner);
                info.SetForeignValue(this.GetValueFor(info));
            }
        }

        public TableReader(bool findAllForeigns)
        {
            this.findAllForeigns = findAllForeigns;
        }

        public TableReader()
        {

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
            if (!this.FindAllForeigns)
                return;

            object foreignKey = reader[column.ForeignKey];
            if (foreignKey != DBNull.Value)
                foreignKeyToLoad.Enqueue(new ForeignInfo(obj, foreignKey, column));
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

        private class ForeignTable : IEquatable<ForeignTable>
        {
            public string TableName { get; }
            public object KeyValue { get; }

            public ForeignTable(ForeignInfo info)
            {
                this.TableName = GetTable(info.ForeignColumn.Type).Name;
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

            public ForeignInfo(object owner, object foreignKey, ColumnInfo foreignColumn)
            {
                this.Owner = owner;
                this.ForeignKey = foreignKey;
                this.ForeignColumn = foreignColumn;
            }

            public void SetForeignValue(object value)
            {
                this.ForeignColumn.SetRaw(this.Owner, value);
            }
        }
    }
}
