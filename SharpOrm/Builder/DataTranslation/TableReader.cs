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

        public TableReader() : base(Connection.ConnectionCreator.Default.Config)
        {

        }

        public TableReader(IQueryConfig config) : base(config)
        {

        }

        public override void LoadForeignKeys()
        {
            while (this.FindForeigns && foreignKeyToLoad.Count > 0)
            {
                ForeignInfo info = foreignKeyToLoad.Dequeue();
                if (this.maxDepth > 0 && info.Depth > this.currentDepth)
                    this.currentDepth = info.Depth;

                if (info.Depth <= this.maxDepth)
                    info.SetForeignValue(this.GetValueFor(info));
            }
        }

        private object GetValueFor(ForeignInfo info)
        {
            using (var query = this.CreateQuery(info.TableName))
            {
                query.Where("id", info.ForeignKey);
                query.Limit = 1;

                using (var reader = query.ExecuteReader())
                    return reader.Read() ? this.ParseFromReader(info.Type, reader, "") : null;
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
                string name = column.AutoGenerated ? column.Name : fullName;
                var index = reader.GetIndexOf(name);
                if (!(index < 0 && column.IsNative))
                    LoadColumnValue(obj, column, reader, index, name, fullName);
            }

            return obj;
        }

        /// <summary>
        /// Loads the column value from the database reader into the object.
        /// </summary>
        /// <param name="owner">The object to load the column value into.</param>
        /// <param name="column">The column information.</param>
        /// <param name="reader">The database reader.</param>
        /// <param name="fullName">The full column name.</param>
        private void LoadColumnValue(object owner, ColumnInfo column, DbDataReader reader, int index, string name, string fullName)
        {
            if (column.IsNative) column.Set(owner, this.ReadDbObject(reader[index]));
            else if (column.IsForeignKey) this.EnqueueForeign(owner, reader.Get(column.ForeignKey), column);
            else column.SetRaw(owner, ParseFromReader(column.Type, reader, fullName));
        }

        private void EnqueueForeign(object owner, object fkValue, ColumnInfo column)
        {
            if (!this.FindForeigns)
            {
                if (this.CreateForeignIfNoDepth)
                    column.SetRaw(owner, GetObjWithKey(column.Type, fkValue));
                return;
            }

            if (!this.CanFindForeign(TableInfo.GetNameOf(column.Type)))
                return;

            var info = this.foreignKeyToLoad.FirstOrDefault(fki => fki.IsFk(column.Type, fkValue));
            if (info == null)
                foreignKeyToLoad.Enqueue(info = new ForeignInfo(column.Type, fkValue, this.currentDepth + 1));

            if (this.currentDepth + 1 > this.currentDepth)
                info.AddFkColumn(owner, column);
        }

        private object GetObjWithKey(Type tableType, object fk)
        {
            var fkTable = GetTable(tableType);
            object value = fkTable.CreateInstance();
            fkTable.Columns.FirstOrDefault(c => c.Key)?.Set(value, fk);

            return value;
        }

        private bool CanFindForeign(string name)
        {
            if (this.foreignsTables == null)
                return false;

            if (this.foreignsTables.Length == 0)
                return true;

            name = name.ToLower();
            return this.foreignsTables.Any(t => t.ToLower().Equals(name));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            foreignKeyToLoad.Clear();
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

        private class ForeignInfo
        {
            public object ForeignKey { get; }
            public string TableName { get; }
            public Type Type { get; }
            public int Depth { get; }

            private Dictionary<object, ColumnInfo> fkObjs = new Dictionary<object, ColumnInfo>();

            public ForeignInfo(Type type, object foreignKey, int depth)
            {
                this.TableName = TableInfo.GetNameOf(type);
                this.ForeignKey = foreignKey;
                this.Type = type;
                this.Depth = depth;
            }

            public void AddFkColumn(object owner, ColumnInfo column)
            {
                this.fkObjs.Add(owner, column);
            }

            public void SetForeignValue(object value)
            {
                foreach (var item in fkObjs)
                    item.Value.SetRaw(item.Key, value);
            }

            public bool IsFk(Type type, object foreignKey)
            {
                return this.Type == type && this.ForeignKey.Equals(foreignKey);
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
                        kv.Value.Set(owner, this.reader.ReadDbObject(reader[kv.Key]));

                    this.LoadFkObjs(owner, reader);
                    return owner;
                }

                var columns = new List<ColumnInfo>(table.Columns.Where(c => !c.IsForeignKey));
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i).ToLower();
                    if (columns.FirstOrDefault(c => name == c.Name.ToLower()) is ColumnInfo ci)
                    {
                        ci.Set(owner, this.reader.ReadDbObject(reader[i]));
                        columns.Remove(ci);
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
