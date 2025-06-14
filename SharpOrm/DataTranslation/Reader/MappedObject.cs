using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Represents an object that can be mapped from a database record.
    /// </summary>
    public class MappedObject : IMappedObject
    {
        private readonly List<MappedObject> _childrens = new List<MappedObject>();
        private readonly List<MappedColumn> _columns = new List<MappedColumn>();
        private readonly TranslationRegistry _registry;
        private readonly object _lock = new object();
        private ObjectActivator _objectActivator;
        private readonly NestedMode _nestedMode;
        private readonly IFkQueue _fkQueue;
        private ColumnInfo _parentColumn;
        private MappedObject _parent;

        /// <summary>
        /// Gets the type of the mapped object.
        /// </summary>
        public Type Type { get; }

        private object instance;

        /// <summary>
        /// Reads and maps an object of type <typeparamref name="T"/> from the database record.
        /// </summary>
        /// <typeparam name="T">The type of the object to read and map.</typeparam>
        /// <param name="record">The database record.</param>
        /// <param name="registry">The translation registry. If null, the default registry is used.</param>
        /// <returns>The mapped object of type <typeparamref name="T"/>.</returns>
        public static T Read<T>(IDataRecord record, TranslationRegistry registry = null)
        {
            return (T)Create(record, typeof(T), registry: registry).Read(record);
        }

        /// <summary>
        /// Creates an <see cref="IMappedObject"/> for the specified type.
        /// </summary>
        /// <param name="record">The database record.</param>
        /// <param name="type">The type of the object to create.</param>
        /// <param name="enqueueable">The foreign key queue. If null, a default queue is used.</param>
        /// <param name="registry">The translation registry. If null, the default registry is used.</param>
        /// <returns>An <see cref="IMappedObject"/> for the specified type.</returns>
        public static IMappedObject Create(IDataRecord record, Type type, IFkQueue enqueueable = null, TranslationRegistry registry = null)
        {
            return Create(record, type, NestedMode.Attribute, enqueueable, registry);
        }

        /// <summary>
        /// Creates an <see cref="IMappedObject"/> for the specified type.
        /// </summary>
        /// <param name="record">The database record.</param>
        /// <param name="type">The type of the object to create.</param>
        /// <param name="enqueueable">The foreign key queue. If null, a default queue is used.</param>
        /// <param name="registry">The translation registry. If null, the default registry is used.</param>
        /// <param name="nestedMode">The nested mode to use for mapping.</param>
        /// <returns>An <see cref="IMappedObject"/> for the specified type.</returns>
        public static IMappedObject Create(IDataRecord record, Type type, NestedMode nestedMode, IFkQueue enqueueable = null, TranslationRegistry registry = null)
        {
            if (registry == null)
                registry = TranslationRegistry.Default;

            if (ReflectionUtils.IsDynamic(type))
                return new MappedDynamic(record, registry);

            if (registry.GetManualMap(type) is TableInfo table)
                return new MappedManualObj(table, registry, record);

            return new MappedObject(type, registry, enqueueable ?? new ObjIdFkQueue(), nestedMode).Map(record, string.Empty);
        }

        private MappedObject(Type type, TranslationRegistry registry, IFkQueue enqueueable, NestedMode nestedMode)
        {
            Type = type;
            _registry = registry;
            _fkQueue = enqueueable;
            _nestedMode = nestedMode;
        }

        private MappedObject Map(IDataRecord record, string prefix)
        {
            if (Type == typeof(Row))
                return this;

            if (!Type.IsArray)
                _objectActivator = new ObjectActivator(Type, record, _registry);

            var register = (_fkQueue as FkLoaders)?.ForeignKeyRegister;

            foreach (var column in _registry.GetTable(Type).Columns)
                if (column.ForeignInfo != null && (!(register?.Exists(column.column) ?? false)))
                    AddIfValidId(record, prefix + column.ForeignInfo.ForeignKey, column, true);
                else if (NeedMapAsValue(column))
                    AddIfValidId(record, GetName(column, prefix), column);
                else
                    MapNested(column, record, prefix);

            if (register != null)
                foreach (var node in register.Nodes)
                    RegisterForeignNode(node, record);

            return this;
        }

        private void RegisterForeignNode(ForeignKeyTreeNode node, IDataRecord record)
        {
            if (node.ColumnInfo.ForeignInfo != null && ReflectionUtils.IsCollection(node.ColumnInfo.Type))
            {
                int index = record.GetIndexOf(node.ColumnInfo.ForeignInfo.ForeignKey);
                AddOrUpdateColumn(new MappedFkColumn(_registry, _fkQueue, node.ColumnInfo, index));
                return;
            }

            var nodeObj = new MappedObject(node.TableInfo.Type, _registry, _fkQueue, _nestedMode) { _parentColumn = node.ColumnInfo, _parent = this };
            nodeObj.MapNodeColumns(node, record);

            _childrens.Add(nodeObj);

            foreach (var subNode in node.Nodes)
                nodeObj.RegisterForeignNode(subNode, record);
        }

        private MappedObject MapNodeColumns(ForeignKeyTreeNode node, IDataRecord record)
        {
            _objectActivator = new ObjectActivator(Type, record, _registry);
            foreach (var info in node.Columns)
                if (info.ForeignInfo != null && !node.Exists(info.ColumnInfo.column))
                    AddIfValidId(record, info.Alias + info.ForeignInfo.ForeignKey, info.ColumnInfo, true);
                else if (NeedMapAsValue(info.ColumnInfo))
                    AddIfValidId(record, info.Alias, info.ColumnInfo);
                else
                    MapNested(info.ColumnInfo, record, info.Alias);

            return this;
        }

        private void MapNested(ColumnInfo column, IDataRecord record, string prefix)
        {
            if (!IsValidNested(column))
                return;

            _childrens.Add(new MappedObject(column.Type, _registry, _fkQueue, _nestedMode) { _parentColumn = column, _parent = this }
                    .Map(record, GetColumnPrefix(column, prefix)));
        }

        private static string GetColumnPrefix(ColumnInfo column, string prefix)
        {
            if (!string.IsNullOrEmpty(column.MapNested?.Prefix))
                return column.MapNested.Prefix;

            if (string.IsNullOrEmpty(prefix))
                return column.Name + '_';

            return prefix;
        }

        private bool IsValidNested(ColumnInfo column)
        {
            return column.MapNested != null ||
                (_nestedMode == NestedMode.All && !IsRootType(column) && !ReflectionUtils.IsCollection(column.Type));
        }

        private bool IsRootType(ColumnInfo column)
        {
            return column.Type == Type;
        }

        private static bool NeedMapAsValue(ColumnInfo column)
        {
            if (column.Translation == null)
                return false;

            return column.IsNative || !(column.Translation is NativeSqlTranslation);
        }

        private void AddIfValidId(IDataRecord record, string name, ColumnInfo column, bool isFk = false)
        {
            int index = record.GetIndexOf(name);
             if (index < 0)
                return;

            AddOrUpdateColumn(isFk ? new MappedUnusedFkColumn(_registry, _fkQueue, column, index) : new MappedColumn(column, index));
        }

        private bool NeedLoadForeign(ColumnInfo column)
        {
            return column.ForeignInfo != null &&
                !ReflectionUtils.IsCollection(column.Type) &&
                _fkQueue is FkLoaders fkLoader &&
                fkLoader.Config.LoadForeign;
        }

        private void AddOrUpdateColumn(MappedColumn column)
        {
            _columns.Remove(column);
            _columns.Add(column);
        }

        private static string GetName(ColumnInfo column, string prefix)
        {
            return column.AutoGenerated && prefix.Length != 0 ? (prefix + column.Name) : column.Name;
        }

        public object Read(IDataRecord record)
        {
            lock (_lock)
            {
                if (Type == typeof(Row))
                    return record.ReadRow(_registry);

                NewObject(record);

                for (int i = 0; i < record.FieldCount; i++)
                    SetValue(i, record[i]);

                return instance;
            }
        }

        private object NewObject(IDataRecord record)
        {
            instance = _objectActivator.CreateInstance(record);

            for (int i = 0; i < _childrens.Count; i++)
            {
                var children = _childrens[i];
                if (!children.Type.IsArray)
                    children._parentColumn.SetRaw(children._parent.instance, children.NewObject(record));
            }

            return instance;
        }

        private void SetValue(int index, object value)
        {
            for (int i = 0; i < _columns.Count; i++)
            {
                var column = _columns.ElementAt(i);

                if (column.Index == index)
                    column.Set(instance, value);
            }

            for (int i = 0; i < _childrens.Count; i++)
                _childrens[i].SetValue(index, value);
        }

        public override string ToString()
        {
            return Type.ToString();
        }

        private class MappedColumn
        {
            public ColumnInfo Column { get; }
            public int Index { get; }

            public MappedColumn(ColumnInfo column, int index)
            {
                Column = column;
                Index = index;
            }

            public virtual void Set(object owner, object value)
            {
                Column.Set(owner, value);
            }

            public override int GetHashCode()
            {
                return Column.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return Column.column.Equals(obj);
            }

            public override string ToString()
            {
                return Column.ToString();
            }
        }

        private class MappedUnusedFkColumn : MappedColumn
        {
            private readonly TranslationRegistry _registry;
            private readonly IFkQueue _fkQueue;

            public MappedUnusedFkColumn(TranslationRegistry registry, IFkQueue fkQueue, ColumnInfo column, int index) : base(column, index)
            {
                _fkQueue = fkQueue;
                _registry = registry;
            }

            public override void Set(object owner, object value)
            {
                Column.SetRaw(owner, ObjIdFkQueue.MakeObjWithId(_registry, Column, value));
            }
        }

        private class MappedFkColumn : MappedColumn
        {
            private readonly IFkQueue _fkQueue;
            private readonly TranslationRegistry _registry;

            public MappedFkColumn(TranslationRegistry registry, IFkQueue fkQueue, ColumnInfo column, int index) : base(column, index)
            {
                _fkQueue = fkQueue;
                _registry = registry;
            }

            public override void Set(object owner, object value)
            {
                _fkQueue.EnqueueForeign(owner, _registry, value, Column);
            }
        }
    }
}
