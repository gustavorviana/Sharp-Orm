using SharpOrm.Builder.DataTranslation.Reader;
using SharpOrm.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace SharpOrm.Builder.DataTranslation
{
    internal class FkLoaders : IFkQueue
    {
        #region Fields/Props
        private readonly Queue<ForeignInfo> foreignKeyToLoad = new Queue<ForeignInfo>();
        private readonly CancellationToken token;
        private readonly LambdaColumn[] fkToLoad;
        private readonly QueryConfig config;

        public DbTransaction Transaction { get; set; }
        public DbConnection Connection { get; set; }
        #endregion

        public FkLoaders(QueryConfig config, LambdaColumn[] fkToLoad, CancellationToken token)
        {
            this.token = token;
            this.config = config;
            this.fkToLoad = fkToLoad;
        }

        public void EnqueueForeign(object owner, object fkValue, ColumnInfo column)
        {
            if (fkValue is null || fkValue is DBNull)
                return;

            if (this.fkToLoad.FirstOrDefault(f => f.IsSame(column)) is LambdaColumn lCol)
                this.AddFkColumn(lCol, owner, fkValue, column);
            else if (this.config.LoadForeign)
                column.SetRaw(owner, ObjIdFkQueue.MakeObjWithId(column, fkValue));
        }

        /// <summary>
        /// Reads the foreign keys of the objects.
        /// </summary>
        public void LoadForeigns()
        {
            while (!token.IsCancellationRequested && foreignKeyToLoad.Count > 0)
            {
                ForeignInfo info = foreignKeyToLoad.Dequeue();
                info.SetForeignValue(this.GetValueFor(info));
            }

            token.ThrowIfCancellationRequested();
        }

        private object GetValueFor(ForeignInfo info)
        {
            if (info is HasManyInfo manyInfo)
                return this.GetCollectionValueFor(manyInfo);

            using (var query = this.CreateQuery(info))
            {
                query.Limit = 1;

                using (var @enum = this.CreateEnumerator(info, query.ExecuteReader()))
                    return @enum.MoveNext() ? @enum.Current : null;
            }
        }

        private object GetCollectionValueFor(HasManyInfo info)
        {
            using (var query = this.CreateQuery(info))
            {
                IList collection = ReflectionUtils.CreateList(info.Type);

                using (var @enum = this.CreateEnumerator(info, query.ExecuteReader()))
                    while (@enum.MoveNext())
                        collection.Add(@enum.Current);

                if (info.Type.IsArray)
                    return ReflectionUtils.ToArray(info.Type.GetElementType(), collection);

                return collection;
            }
        }

        private Query CreateQuery(ForeignInfo info)
        {
            var query = this.CreateQuery(info.TableName);
            query.Where(info is HasManyInfo many ? many.LocalKey : "Id", info.ForeignKey);
            return query;
        }

        private Query CreateQuery(string name)
        {
            if (this.Transaction != null)
                return new Query(this.Transaction, this.config, name) { Token = this.token };

            if (this.Connection != null)
                return new Query(this.Connection, this.config, name, false) { Token = this.token };

            return new Query(this.config, name) { Token = this.token };
        }

        private DbObjectEnumerator CreateEnumerator(ForeignInfo info, DbDataReader reader)
        {
            var mapped = MappedObject.Create(reader, ReflectionUtils.GetGenericArg(info.Type), this, config.Translation);
            return new DbObjectEnumerator(reader, mapped, token);
        }

        private void AddFkColumn(LambdaColumn lCol, object owner, object fkValue, ColumnInfo column)
        {
            var info = this.foreignKeyToLoad.FirstOrDefault(fki => fki.IsFk(column.Type, fkValue));
            if (info == null)
            {
                info = column.IsMany ? new HasManyInfo(lCol, fkValue, column.LocalKey) : new ForeignInfo(lCol, fkValue);
                foreignKeyToLoad.Enqueue(info);
            }

            info.AddFkColumn(owner, column);
        }
    }
}
