using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Represents an object that can be mapped from a database record.
    /// </summary>
    public class MappedObject : IMappedObject
    {
        #region Properties\Fields
        private readonly List<MappedObject> childrens = new List<MappedObject>();
        private readonly List<MappedColumn> fkColumns = new List<MappedColumn>();
        private readonly List<MappedColumn> columns = new List<MappedColumn>();
        private readonly TranslationRegistry registry;
        private readonly object _lock = new object();
        private ObjectActivator objectActivator;
        private readonly NestedMode nestedMode;
        private readonly IFkQueue enqueueable;
        private ColumnInfo parentColumn;
        private MappedObject parent;

        /// <summary>
        /// Gets the type of the mapped object.
        /// </summary>
        public Type Type { get; }

        private object instance;
        #endregion

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

            return new MappedObject(type, registry, enqueueable ?? new ObjIdFkQueue(), nestedMode).Map(registry, record, "");
        }

        private MappedObject(Type type, TranslationRegistry registry, IFkQueue enqueueable, NestedMode nestedMode)
        {
            this.Type = type;
            this.registry = registry;
            this.enqueueable = enqueueable;
            this.nestedMode = nestedMode;
        }

        private MappedObject Map(TranslationRegistry registry, IDataRecord record, string prefix)
        {
            if (this.Type == typeof(Row))
                return this;

            if (!Type.IsArray)
                objectActivator = new ObjectActivator(Type, record, registry);

            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("_"))
                prefix += '_';

            foreach (var column in registry.GetTable(this.Type).Columns)
                if (column.ForeignInfo != null) AddIfValidId(record, fkColumns, column.ForeignInfo.ForeignKey, column);
                else if (NeedMapAsValue(column)) AddIfValidId(record, columns, GetName(column, prefix), column);
                else MapNested(column, record, prefix);

            return this;
        }

        private void MapNested(ColumnInfo column, IDataRecord record, string prefix)
        {
            if (IsValidNested(column))
                MapChild(record, column, prefix);
        }

        private bool IsValidNested(ColumnInfo column)
        {
            return column.MapNested ||
                (nestedMode == NestedMode.All && !IsRootType(column) && !ReflectionUtils.IsCollection(column.Type));
        }

        private bool IsRootType(ColumnInfo column)
        {
            return column.Type == Type;
        }

        private void MapChild(IDataRecord record, ColumnInfo column, string prefix)
        {
            childrens.Add(new MappedObject(column.Type, this.registry, enqueueable, nestedMode) { parentColumn = column, parent = this }
                    .Map(registry, record, prefix + column.Name));
        }

        private static bool NeedMapAsValue(ColumnInfo column)
        {
            if (column.Translation == null)
                return false;

            return column.IsNative || !(column.Translation is NativeSqlTranslation);
        }

        private void AddIfValidId(IDataRecord record, List<MappedColumn> columns, string name, ColumnInfo column)
        {
            int index = record.GetIndexOf(name);
            if (index >= 0)
                columns.Add(new MappedColumn(column, index));
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
                    return record.ReadRow(registry);

                NewObject(record);

                for (int i = 0; i < record.FieldCount; i++)
                    SetValue(i, record[i]);

                return instance;
            }
        }

        private object NewObject(IDataRecord record)
        {
            instance = objectActivator.CreateInstance(record);

            for (int i = 0; i < childrens.Count; i++)
            {
                var children = childrens[i];
                if (!children.Type.IsArray)
                    children.parentColumn.SetRaw(children.parent.instance, children.NewObject(record));
            }

            return instance;
        }

        private void SetValue(int index, object value)
        {
            if (enqueueable != null)
                EnqueueFk(index, value);

            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];

                if (column.Index == index)
                    column.Set(instance, value);
            }

            for (int i = 0; i < childrens.Count; i++)
                childrens[i].SetValue(index, value);
        }

        private void EnqueueFk(int index, object value)
        {
            foreach (var column in fkColumns)
                if (column.Index == index)
                    enqueueable.EnqueueForeign(instance, registry, value, column.Column);
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

            public void Set(object owner, object value)
            {
                Column.Set(owner, value);
            }
        }
    }
}
