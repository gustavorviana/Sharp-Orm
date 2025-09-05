using SharpOrm.Builder;
using SharpOrm.Builder.Tables;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// **Obsolete:** This class is deprecated and will be replaced by <see cref="Reader.ObjectRecordReader"/> in version 4.0.
    /// Represents an object that can be mapped from a database record.
    /// </summary>
    [Obsolete("MappedObject is deprecated and will be replaced by ObjectRecordMapper in version 4.0.")]
    public class MappedObject : IMappedObject
    {
        private readonly List<MappedObject> _childrens = new List<MappedObject>();
        private readonly List<MappedColumn> _columns = new List<MappedColumn>();
        private readonly TranslationRegistry _registry;
        private readonly object _lock = new object();
        private ObjectActivator _objectActivator;
        private readonly IFkQueue _fkQueue;
        private ColumnInfo _parentColumn;
        private MappedObject _parent;
        private bool IsCollection { get; }

        internal ForeignKeyNodeBase Node { get; set; }

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
        [Obsolete("It will be removed in version 4.0.")]
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
        [Obsolete("It will be removed in version 4.0.")]
        public static IMappedObject Create(IDataRecord record, Type type, NestedMode nestedMode, IFkQueue enqueueable = null, TranslationRegistry registry = null)
        {
            if (registry == null)
                registry = TranslationRegistry.Default;

            if (type == typeof(Row))
                return new MappedRowObject(registry);

            if (ReflectionUtils.IsDynamic(type))
                return new MappedDynamic(record, registry);

            return Create(record, type, enqueueable, (enqueueable as FkLoaders)?.ForeignKeyNode, registry);
        }

        internal static IMappedObject Create(IDataRecord record, Type type, IFkQueue enqueueable, ForeignKeyNodeBase node, TranslationRegistry registry)
        {
            return new MappedObject(type, registry, enqueueable ?? new ObjIdFkQueue())
            {
                Node = node
            }.Map(record);
        }

        internal MappedObject(Type type, TranslationRegistry registry, IFkQueue enqueueable)
        {
            Type = type;
            IsCollection = RuntimeList.IsCollection(type);
            _registry = registry;
            _fkQueue = enqueueable;
        }

        internal MappedObject Map(IDataRecord record)
        {
            var table = _registry.GetTable(Type);

            InitActivator(record);

            foreach (var node in table.Columns.Nodes)
                MapNodeChildrens(record, node);

            if (Node != null)
                foreach (var node in Node.Nodes)
                    RegisterForeignNode(node, record);

            return this;
        }

        private void InitActivator(IDataRecord record)
        {
            if (!IsCollection && _objectActivator == null)
                _objectActivator = new ObjectActivator(Type, record, _registry);
        }

        private void MapNodeChildrens(IDataRecord record, IColumnNode node)
        {
            if (node.Nodes.Count == 0)
            {
                MapNode(record, node);
                return;
            }

            //var owner = new MappedObject(node.Column.Type, _registry, _fkQueue) { _parentColumn = node.Column, _parent = this };
            //owner.InitActivator(record);

            foreach (var children in node.Nodes)
                MapNodeChildrens(record, children);

            //_childrens.Add(owner);
        }

        private void MapNode(IDataRecord record, IColumnNode node)
        {
            if (node.Column.ForeignInfo != null && NodeExists(node))
                AddIfValidId(record, node.Column.ForeignInfo.ForeignKey, node.Column, true);
            else if (NeedMapAsValue(node.Column))
                AddIfValidId(record, node.Column.Name, node.Column);
        }

        private bool NodeExists(IColumnNode node)
        {
            return !Node?.Exists(node.Column._column) ?? false;
        }

        private void RegisterForeignNode(ForeignKeyNode node, IDataRecord record)
        {
            if (node.ColumnInfo.ForeignInfo != null && ReflectionUtils.IsCollection(node.ColumnInfo.Type))
            {
                int index = record.GetIndexOf(node.ColumnInfo.ForeignInfo.ForeignKey);
                AddOrUpdateColumn(new MappedFkColumn(_registry, _fkQueue, node, index));
                return;
            }

            var nodeObj = new MappedObject(node.ColumnInfo.Type, _registry, _fkQueue) { _parentColumn = node.ColumnInfo, _parent = this };
            nodeObj.MapNodeColumns(node, record);

            _childrens.Add(nodeObj);

            foreach (var subNode in node.Nodes)
                nodeObj.RegisterForeignNode(subNode, record);
        }

        private MappedObject MapNodeColumns(ForeignKeyNode node, IDataRecord record)
        {
            InitActivator(record);

            foreach (var info in node.Columns)
                if (info.ForeignInfo != null && !node.Exists(info.ColumnInfo._column))
                    AddIfValidId(record, info.Alias + info.ForeignInfo.ForeignKey, info.ColumnInfo, true);
                else if (NeedMapAsValue(info.ColumnInfo))
                    AddIfValidId(record, info.Alias, info.ColumnInfo);

            return this;
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

        private void AddOrUpdateColumn(MappedColumn column)
        {
            _columns.Remove(column);
            _columns.Add(column);
        }

        public object Read(IDataRecord record)
        {
            lock (_lock)
            {
                NewObject(record);

                for (int i = 0; i < record.FieldCount; i++)
                    SetValue(i, record[i]);

                return instance;
            }
        }

        private object NewObject(IDataRecord record)
        {
            instance = _objectActivator.CreateInstance(record);
            if (instance == null)
                return null;

            for (int i = 0; i < _childrens.Count; i++)
            {
                var children = _childrens[i];
                if (!children.IsCollection)
                    children._parentColumn.SetRaw(children._parent.instance, children.NewObject(record));
            }

            return instance;
        }

        private void SetValue(int index, object value)
        {
            if (instance == null)
                return;

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
            if (_parentColumn == null)
                return Type.ToString();

            return $"{_parentColumn.Name}: {Type}";
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
                return Column._column.Equals(obj);
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
            private readonly ForeignKeyNode _node;

            public MappedFkColumn(TranslationRegistry registry, IFkQueue fkQueue, ForeignKeyNode node, int index) : base(node.ColumnInfo, index)
            {
                _node = node;
                _fkQueue = fkQueue;
                _registry = registry;
            }

            public override void Set(object owner, object value)
            {
                _fkQueue.EnqueueForeign(owner, _registry, value, _node);
            }
        }
    }
}
