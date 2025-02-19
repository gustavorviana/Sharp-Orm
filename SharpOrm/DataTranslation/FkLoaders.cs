using SharpOrm.Builder;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.DataTranslation.Reader;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace SharpOrm.DataTranslation
{
    internal class FkLoaders : IFkQueue
    {
        #region Fields/Props
        private readonly Queue<ForeignInfo> _foreignKeyToLoad = new Queue<ForeignInfo>();
        private readonly CancellationToken _token;
        private readonly IReadOnlyList<MemberInfo> _fkToLoad;

        public ConnectionManager Manager { get; }
        #endregion

        public FkLoaders(ConnectionManager manager, IReadOnlyList<MemberInfo> fkToLoad, CancellationToken token)
        {
            _token = token;
            Manager = manager;
            _fkToLoad = fkToLoad;
        }

        public void EnqueueForeign(object owner, TranslationRegistry translator, object fkValue, ColumnInfo column)
        {
            if (fkValue is null || fkValue is DBNull)
                return;

            if (_fkToLoad.FirstOrDefault(f => f == column.column) is MemberInfo lCol)
                AddFkColumn(lCol, owner, fkValue, column);
            else if (Manager.Config.LoadForeign)
                column.SetRaw(owner, ObjIdFkQueue.MakeObjWithId(translator, column, fkValue));
        }

        /// <summary>
        /// Reads the foreign keys of the objects.
        /// </summary>
        public void LoadForeigns()
        {
            while (!_token.IsCancellationRequested && _foreignKeyToLoad.Count > 0)
            {
                ForeignInfo info = _foreignKeyToLoad.Dequeue();
                info.SetForeignValue(GetValueFor(info));
            }

            _token.ThrowIfCancellationRequested();
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
                var runtime = new RuntimeList(info.Type);

                using (var @enum = CreateEnumerator(info, query.ExecuteReader()))
                    runtime.AddAll(@enum);

                return runtime.ToCollection(info.Type);
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
            return new Query(name, Manager) { Token = _token };
        }

        private DbObjectEnumerator CreateEnumerator(ForeignInfo info, DbDataReader reader)
        {
            var mapped = MappedObject.Create(reader, ReflectionUtils.GetGenericArg(info.Type), Manager.Config.NestedMapMode, this, Manager.Config.Translation);
            return new DbObjectEnumerator(reader, mapped, _token);
        }

        private void AddFkColumn(MemberInfo lCol, object owner, object fkValue, ColumnInfo column)
        {
            var info = _foreignKeyToLoad.FirstOrDefault(fki => fki.IsFk(column.Type, fkValue));
            if (info == null)
                _foreignKeyToLoad.Enqueue(info = new ForeignInfo(lCol, fkValue, column.ForeignInfo.LocalKey));

            info.AddFkColumn(owner, column);
        }
    }
}
