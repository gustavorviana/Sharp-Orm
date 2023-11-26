﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    internal class MappedObject
    {
        #region Properties\Fields
        private readonly List<MappedObject> childrens = new List<MappedObject>();
        private readonly List<ColumnInfo> fkColumns = new List<ColumnInfo>();
        private readonly List<ColumnInfo> columns = new List<ColumnInfo>();
        internal readonly TranslationRegistry registry;
        private readonly DbDataReader reader;
        private ColumnInfo parentColumn;
        private MappedObject parent;

        internal Type type;
        private object instance;
        #endregion

        public MappedObject(TranslationRegistry registry, DbDataReader reader, Type type, string prefix = "")
        {
            this.registry = registry;
            this.reader = reader;
            this.type = type;

            if (this.type != typeof(Row))
                this.Map(prefix);
        }

        private void Map(string prefix)
        {
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("_"))
                prefix += '_';

            foreach (var column in TableInfo.GetColumns(this.type, this.registry))
                if (column.IsForeignKey) AddIfValidId(this.fkColumns, column.ForeignKey, column);
                else if (column.IsNative) AddIfValidId(this.columns, GetName(column, prefix), column);
                else this.childrens.Add(new MappedObject(this.registry, this.reader, column.Type, prefix + column.Name) { parentColumn = column, parent = this });
        }

        private void AddIfValidId(List<ColumnInfo> columns, string name, ColumnInfo column)
        {
            int index = this.reader.GetIndexOf(name);
            if (index < 0)
                return;

            column.ReaderIndex = index;
            columns.Add(column);
        }

        private static string GetName(ColumnInfo column, string prefix)
        {
            return column.AutoGenerated && prefix.Length != 0 ? (prefix + column.Name).ToLower() : column.Name.ToLower();
        }

        [Obsolete]
        public object Read(TableReader reader)
        {
            this.NewObject();

            for (int i = 0; i < this.reader.FieldCount; i++)
                this.SetValue(i, this.reader[i], reader);

            return this.instance;
        }

        [Obsolete]
        private void SetValue(int index, object value, TableReader reader)
        {
            this.EnqueueFk(index, value, reader);

            foreach (var column in this.columns)
                if (column.ReaderIndex == index)
                    column.Set(this.instance, value);

            foreach (var children in this.childrens)
                children.SetValue(index, value, reader);
        }

        [Obsolete]
        private void EnqueueFk(int index, object value, TableReader reader)
        {
            foreach (var column in this.fkColumns)
                if (column.ReaderIndex == index)
                    reader.EnqueueForeign(this.instance, value, column);
        }

        public object Read(DbObjectReader reader)
        {
            if (this.type == typeof(Row))
                return this.reader.ReadRow(reader.config);

            this.NewObject();

            for (int i = 0; i < this.reader.FieldCount; i++)
                this.SetValue(i, this.reader[i], reader);

            return this.instance;
        }

        private object NewObject()
        {
            this.instance = Activator.CreateInstance(this.type);

            foreach (var children in this.childrens)
                children.parentColumn.SetRaw(children.parent.instance, children.NewObject());

            return this.instance;
        }

        private void SetValue(int index, object value, DbObjectReader reader)
        {
            this.EnqueueFk(index, value, reader);

            foreach (var column in this.columns)
                if (column.ReaderIndex == index)
                    column.Set(this.instance, value);

            foreach (var children in this.childrens)
                children.SetValue(index, value, reader);
        }

        private void EnqueueFk(int index, object value, DbObjectReader reader)
        {
            foreach (var column in this.fkColumns)
                if (column.ReaderIndex == index)
                    reader.EnqueueForeign(this.instance, value, column);
        }
    }
}
