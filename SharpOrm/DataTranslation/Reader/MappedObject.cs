﻿using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Represents an object that can be mapped from a database reader.
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
        /// Reads and maps an object of type <typeparamref name="T"/> from the database reader.
        /// </summary>
        /// <typeparam name="T">The type of the object to read and map.</typeparam>
        /// <param name="reader">The database reader.</param>
        /// <param name="registry">The translation registry. If null, the default registry is used.</param>
        /// <returns>The mapped object of type <typeparamref name="T"/>.</returns>
        public static T Read<T>(DbDataReader reader, TranslationRegistry registry = null)
        {
            return (T)Create(reader, typeof(T), registry: registry).Read(reader);
        }

        /// <summary>
        /// Creates an <see cref="IMappedObject"/> for the specified type.
        /// </summary>
        /// <param name="reader">The database reader.</param>
        /// <param name="type">The type of the object to create.</param>
        /// <param name="enqueueable">The foreign key queue. If null, a default queue is used.</param>
        /// <param name="registry">The translation registry. If null, the default registry is used.</param>
        /// <param name="prefix">The prefix for column names.</param>
        /// <returns>An <see cref="IMappedObject"/> for the specified type.</returns>
        public static IMappedObject Create(DbDataReader reader, Type type, IFkQueue enqueueable = null, TranslationRegistry registry = null)
        {
            if (registry == null)
                registry = TranslationRegistry.Default;

            if (ReflectionUtils.IsDynamic(type))
                return new MappedDynamic(reader, registry);

            if (TableInfo.TryGetManualMap(type) is TableInfo table)
                return new MappedManualObj(table, registry, reader);

            if (enqueueable == null)
                enqueueable = new ObjIdFkQueue();

            return new MappedObject(type, registry, enqueueable).Map(registry, reader, "");
        }

        private MappedObject(Type type, TranslationRegistry registry, IFkQueue enqueueable)
        {
            Type = type;
            this.registry = registry;
            this.enqueueable = enqueueable;
        }

        private MappedObject Map(TranslationRegistry registry, DbDataReader reader, string prefix)
        {
            objectActivator = new ObjectActivator(Type, reader, registry);

            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("_"))
                prefix += '_';

            foreach (var column in TableInfo.GetColumns(Type, registry))
                if (column.IsForeignKey) AddIfValidId(reader, fkColumns, column.ForeignKey, column);
                else if (NeedMapAsValue(column)) AddIfValidId(reader, columns, GetName(column, prefix), column);
                else childrens.Add(new MappedObject(column.Type, this.registry, enqueueable) { parentColumn = column, parent = this }.Map(registry, reader, prefix + column.Name));

            return this;
        }

        private static bool NeedMapAsValue(ColumnInfo column)
        {
            if (column.Translation is null)
                return false;

            return column.IsNative || !(column.Translation is NativeSqlTranslation);
        }

        private void AddIfValidId(DbDataReader reader, List<MappedColumn> columns, string name, ColumnInfo column)
        {
            int index = reader.GetIndexOf(name);
            if (index >= 0)
                columns.Add(new MappedColumn(column, index));
        }

        private static string GetName(ColumnInfo column, string prefix)
        {
            return column.AutoGenerated && prefix.Length != 0 ? (prefix + column.Name).ToLower() : column.Name.ToLower();
        }

        public object Read(DbDataReader reader)
        {
            lock (_lock)
            {
                if (Type == typeof(Row))
                    return reader.ReadRow(registry);

                NewObject(reader);

                for (int i = 0; i < reader.FieldCount; i++)
                    SetValue(i, reader[i]);

                return instance;
            }
        }

        private object NewObject(DbDataReader reader)
        {
            instance = objectActivator.CreateInstance(reader);

            foreach (var children in childrens)
                children.parentColumn.SetRaw(children.parent.instance, children.NewObject(reader));

            return instance;
        }

        private void SetValue(int index, object value)
        {
            if (enqueueable != null)
                EnqueueFk(index, value);

            foreach (var column in columns)
                if (column.Index == index)
                    column.Set(instance, value);

            foreach (var children in childrens)
                children.SetValue(index, value);
        }

        private void EnqueueFk(int index, object value)
        {
            foreach (var column in fkColumns)
                if (column.Index == index)
                    enqueueable.EnqueueForeign(instance, value, column.Column);
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
