using SharpOrm.Builder;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.DataTranslation.Reader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace SharpOrm.DataTranslation
{
    internal class FkLoaders : IFkQueue
    {
        #region Fields/Props
        private readonly Queue<ForeignInfo> foreignKeyToLoad = new Queue<ForeignInfo>();
        private readonly CancellationToken token;
        private readonly MemberInfoColumn[] fkToLoad;

        public ConnectionManager Manager { get; }
        #endregion

        public FkLoaders(ConnectionManager manager, MemberInfoColumn[] fkToLoad, CancellationToken token)
        {
            this.token = token;
            Manager = manager;
            this.fkToLoad = fkToLoad;
        }

        public void EnqueueForeign(object owner, object fkValue, ColumnInfo column)
        {
            if (fkValue is null || fkValue is DBNull)
                return;

            if (fkToLoad.FirstOrDefault(f => f.IsSame(column)) is MemberInfoColumn lCol)
                AddFkColumn(lCol, owner, fkValue, column);
            else if (Manager.Config.LoadForeign)
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
                info.SetForeignValue(GetValueFor(info));
            }

            token.ThrowIfCancellationRequested();
        }

        private object GetValueFor(ForeignInfo info)
        {
            if (ReflectionUtils.IsCollection(info.Type))
                return GetCollectionValueFor(info);

            using (var query = CreateQuery(info))
            {
                query.Limit = 1;

                using (var @enum = CreateEnumerator(info, query.ExecuteReader()))
                    return @enum.MoveNext() ? @enum.Current : null;
            }
        }

        private object GetCollectionValueFor(ForeignInfo info)
        {
            using (var query = CreateQuery(info))
            {
                IList collection = ReflectionUtils.CreateList(info.Type);

                using (var @enum = CreateEnumerator(info, query.ExecuteReader()))
                    while (@enum.MoveNext())
                        collection.Add(@enum.Current);

                if (info.Type.IsArray)
                    return ReflectionUtils.ToArray(info.Type.GetElementType(), collection);

                return collection;
            }
        }

        private Query CreateQuery(ForeignInfo info)
        {
            var query = CreateQuery(info.TableName);
            query.Where(info.LocalKey ?? "Id", info.ForeignKey);
            return query;
        }

        private Query CreateQuery(string name)
        {
            return new Query(name, Manager) { Token = token };
        }

        private DbObjectEnumerator CreateEnumerator(ForeignInfo info, DbDataReader reader)
        {
            var mapped = MappedObject.Create(reader, ReflectionUtils.GetGenericArg(info.Type), this, Manager.Config.Translation);
            return new DbObjectEnumerator(reader, mapped, token);
        }

        private void AddFkColumn(MemberInfoColumn lCol, object owner, object fkValue, ColumnInfo column)
        {
            var info = foreignKeyToLoad.FirstOrDefault(fki => fki.IsFk(column.Type, fkValue));
            if (info == null)
                foreignKeyToLoad.Enqueue(info = new ForeignInfo(lCol, fkValue, column.ForeignInfo.LocalKey));

            info.AddFkColumn(owner, column);
        }
    }
}
