﻿using System;
using System.Collections.Generic;
using System.Data.Common;

namespace SharpOrm.Builder.DataTranslation.Reader
{
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

        public Type Type { get; }

        private object instance;
        #endregion

        public static T Read<T>(DbDataReader reader, TranslationRegistry registry = null)
        {
            return (T)Create(reader, typeof(T), registry: registry).Read(reader);
        }

        public static IMappedObject Create(DbDataReader reader, Type type, IFkQueue enqueueable = null, TranslationRegistry registry = null, string prefix = "")
        {
            if (registry == null)
                registry = TranslationRegistry.Default;

            if (enqueueable == null)
                enqueueable = new ObjIdFkQueue();

            if (ReflectionUtils.IsDynamic(type))
                return new MappedDynamic(registry, reader);

            return new MappedObject(type, registry, enqueueable).Map(registry, reader, prefix);
        }

        private MappedObject(Type type, TranslationRegistry registry, IFkQueue enqueueable)
        {
            this.Type = type;
            this.registry = registry;
            this.enqueueable = enqueueable;
        }

        private MappedObject Map(TranslationRegistry registry, DbDataReader reader, string prefix)
        {
            objectActivator = new ObjectActivator(this.Type, reader, registry);

            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("_"))
                prefix += '_';

            foreach (var column in TableInfo.GetColumns(this.Type, registry))
                if (column.IsForeignKey) AddIfValidId(reader, this.fkColumns, column.ForeignKey, column);
                else if (NeedMapAsValue(column)) AddIfValidId(reader, this.columns, GetName(column, prefix), column);
                else this.childrens.Add(new MappedObject(column.Type, this.registry, this.enqueueable) { parentColumn = column, parent = this }.Map(registry, reader, prefix + column.Name));

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
            lock (this._lock)
            {
                if (this.Type == typeof(Row))
                    return reader.ReadRow(this.registry);

                this.NewObject(reader);

                for (int i = 0; i < reader.FieldCount; i++)
                    this.SetValue(i, reader[i]);

                return this.instance;
            }
        }

        private object NewObject(DbDataReader reader)
        {
            this.instance = objectActivator.CreateInstance(reader);

            foreach (var children in this.childrens)
                children.parentColumn.SetRaw(children.parent.instance, children.NewObject(reader));

            return this.instance;
        }

        private void SetValue(int index, object value)
        {
            if (this.enqueueable != null)
                this.EnqueueFk(index, value);

            foreach (var column in this.columns)
                if (column.Index == index)
                    column.Set(this.instance, value);

            foreach (var children in this.childrens)
                children.SetValue(index, value);
        }

        private void EnqueueFk(int index, object value)
        {
            foreach (var column in this.fkColumns)
                if (column.Index == index)
                    this.enqueueable.EnqueueForeign(this.instance, value, column.Column);
        }

        private class MappedColumn
        {
            public ColumnInfo Column { get; }
            public int Index { get; }

            public MappedColumn(ColumnInfo column, int index)
            {
                this.Column = column;
                this.Index = index;
            }

            public void Set(object owner, object value)
            {
                this.Column.Set(owner, value);
            }
        }
    }
}
