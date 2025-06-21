using SharpOrm.Builder;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.DataTranslation.Reader;
using SharpOrm.ForeignKey;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

namespace SharpOrm.DataTranslation
{
    [Obsolete]
    internal class FkLoaders : IFkQueue
    {
        #region Fields/Props
        private readonly Queue<ForeignInfo> _foreignKeyToLoad = new Queue<ForeignInfo>();
        private readonly CancellationToken _token;

        public ForeignKeyRegister ForeignKeyRegister { get; }
        public ConnectionManager Manager { get; }
        public QueryConfig Config => Manager.Config;
        #endregion

        public FkLoaders(ConnectionManager manager, ForeignKeyRegister register, CancellationToken token)
        {
            ForeignKeyRegister = register;
            _token = token;

            Manager = manager;
        }

        public void EnqueueForeign(object owner, TranslationRegistry translator, object fkValue, IForeignKeyNode node)
        {
            if (fkValue is null || fkValue is DBNull)
                return;

            AddFkColumn(owner, fkValue, node as ForeignKeyNode);
        }

        /// <summary>
        /// Reads the foreign keys of the objects.
        /// </summary>
        public void LoadForeigns()
        {
            while (!_token.IsCancellationRequested && _foreignKeyToLoad.Count > 0)
            {
                ForeignInfo info = _foreignKeyToLoad.Dequeue();
                info.SetValue(GetCollectionValueFor(info));
            }

            _token.ThrowIfCancellationRequested();
        }

        private RuntimeList GetCollectionValueFor(ForeignInfo info)
        {
            using (var query = CreateQuery(info))
            {
                var runtime = new RuntimeList(info.Type);

                using (var @enum = CreateEnumerator(info, query.ExecuteReader()))
                    runtime.AddAll(@enum);

                return runtime;
            }
        }

        private Query CreateQuery(ForeignInfo info)
        {
            var query = new Query(info.TableName, Manager) { Token = _token };
            info.Node.ApplySelectToQuery(query);

            query.Where(info.LocalKey ?? "Id", info.ForeignKey);
            return query;
        }

        private DbObjectEnumerator CreateEnumerator(ForeignInfo info, DbDataReader reader)
        {
            var mapped = MappedObject.Create(reader, ReflectionUtils.GetGenericArg(info.Type), Manager.Config.NestedMapMode, null, Manager.Config.Translation);
            return new DbObjectEnumerator(reader, mapped, _token);
        }

        private void AddFkColumn(object owner, object fkValue, ForeignKeyNode node)
        {
            _foreignKeyToLoad.Enqueue(new ForeignInfo(owner, node, fkValue));
        }

        private class ForeignInfo
        {
            private readonly object _owner;
            public ForeignKeyNode Node { get; }

            public object ForeignKey { get; }
            public string TableName { get; }
            public string LocalKey { get; }

            public Type Type => Node.ColumnInfo.Type;

            public ForeignInfo(object owner, ForeignKeyNode node, object foreignKey)
            {
                LocalKey = node.ColumnInfo.ForeignInfo.LocalKey;
                TableName = node.Name.Name;
                ForeignKey = foreignKey;
                _owner = owner;
                Node = node;
            }

            public void SetValue(RuntimeList list)
            {
                Node.ColumnInfo.SetRaw(_owner, list.ToCollection(Type));
            }
        }
    }
}
