using SharpOrm.Builder.DataTranslation.Reader;
using SharpOrm.Collections;
using SharpOrm.Connection;
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

        public ConnectionManager Manager { get; }
        #endregion

        public FkLoaders(ConnectionManager manager, LambdaColumn[] fkToLoad, CancellationToken token)
        {
            this.token = token;
            this.Manager = manager;
            this.fkToLoad = fkToLoad;
        }

        public void EnqueueForeign(object owner, object fkValue, ColumnInfo column)
        {
            if (fkValue is null || fkValue is DBNull)
                return;

            if (this.fkToLoad.FirstOrDefault(f => f.IsSame(column)) is LambdaColumn lCol)
                this.AddFkColumn(lCol, owner, fkValue, column);
            else if (this.Manager.Config.LoadForeign)
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
            return new Query(name, this.Manager) { Token = this.token };
        }

        private DbObjectEnumerator CreateEnumerator(ForeignInfo info, DbDataReader reader)
        {
            var mapped = MappedObject.Create(reader, ReflectionUtils.GetGenericArg(info.Type), this, this.Manager.Config.Translation);
            return new DbObjectEnumerator(reader, mapped, token, false);
        }

        private void AddFkColumn(LambdaColumn lCol, object owner, object fkValue, ColumnInfo column)
        {
            var info = this.foreignKeyToLoad.FirstOrDefault(fki => fki.IsFk(column.Type, fkValue));
            if (info == null)
            {
                info = column.HasManyInfo != null ? new HasManyInfo(lCol, fkValue, column.HasManyInfo.LocalKey) : new ForeignInfo(lCol, fkValue);
                foreignKeyToLoad.Enqueue(info);
            }

            info.AddFkColumn(owner, column);
        }
    }
}
