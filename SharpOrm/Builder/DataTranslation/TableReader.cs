using SharpOrm.Builder.DataTranslation.Reader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation
{
    /// <summary>
    /// Represents a table translator for database table operations.
    /// </summary>
    [Obsolete("Use SharpOrm.Builder.DataTranslation.DbObjectReader instead. It will be removed in version 2.x.x.")]
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
            if (info is HasManyInfo manyInfo)
                return this.GetCollectionValueFor(manyInfo);

            using (var query = this.CreateQuery(info.TableName))
            {
                query.Where("id", info.ForeignKey);
                query.Limit = 1;

                using (var reader = query.ExecuteReader())
                    return reader.Read() ? this.ParseFromReader(info.Type, reader, "") : null;
            }
        }

        private object GetCollectionValueFor(HasManyInfo info)
        {
            using (var query = this.CreateQuery(info.TableName))
            {
                query.Where(info.LocalKey, info.ForeignKey);

                Type argType = ReflectionUtils.GetGenericArg(info.Type);
                IList collection = ReflectionUtils.CreateList(info.Type);

                using (var reader = query.ExecuteReader())
                    foreach (var item in this.GetEnumerable(reader, argType))
                        collection.Add(item);

                if (info.Type.IsArray)
                    return ReflectionUtils.ToArray(info.Type.GetElementType(), collection);

                return collection;
            }
        }

        /// <inheritdoc />
        protected override object ParseFromReader(Type typeToParse, DbDataReader reader, string prefix)
        {
            return MappedObject.Create(reader, typeToParse, null, prefix).Read(reader, this);
        }

        internal void EnqueueForeign(object owner, object fkValue, ColumnInfo column)
        {
            if (fkValue is null || fkValue is DBNull)
                return;

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
                foreignKeyToLoad.Enqueue(info = this.GetForeign(fkValue, column));

            if (this.currentDepth + 1 > this.currentDepth)
                info.AddFkColumn(owner, column);
        }

        private ForeignInfo GetForeign(object fkValue, ColumnInfo column)
        {
            int depth = this.currentDepth + 1;
            return column.IsMany ? new HasManyInfo(column.Type, fkValue, depth, column.LocalKey) : new ForeignInfo(column.Type, fkValue, depth);
        }

        private object GetObjWithKey(Type tableType, object fk)
        {
            var fkTable = TableInfo.Get(tableType);
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

        public override IEnumerable<T> GetEnumerable<T>(DbDataReader reader)
        {
            return this.GetEnumerable(reader, typeof(T)).OfType<T>();
        }

        private IEnumerable<object> GetEnumerable(DbDataReader reader, Type type)
        {
            if (type == typeof(Row))
            {
                while (!this.Token.IsCancellationRequested && reader.Read())
                    yield return this.GetRow(reader);

                this.Token.ThrowIfCancellationRequested();
                yield break;
            }

            var objReader = MappedObject.Create(reader, type);

            while (!this.Token.IsCancellationRequested && reader.Read())
                yield return objReader.Read(reader, this);

            this.Token.ThrowIfCancellationRequested();
        }
    }
}
