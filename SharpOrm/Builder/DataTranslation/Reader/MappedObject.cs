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
        private readonly Dictionary<int, ColumnInfo> fkColumns = new Dictionary<int, ColumnInfo>();
        private readonly Dictionary<int, ColumnInfo> columns = new Dictionary<int, ColumnInfo>();
        private readonly List<MappedObject> childrens = new List<MappedObject>();
        private readonly TranslationRegistry registry;
        private readonly DbDataReader reader;
        private ColumnInfo parentColumn;
        private MappedObject parent;

        private Type type;
        private object instance;
        public object Instance => this.instance;
        #endregion

        public MappedObject(TranslationRegistry registry, DbDataReader reader, Type type, string prefix)
        {
            this.registry = registry;
            this.reader = reader;
            this.type = type;
            this.Map(prefix);
        }

        private void Map(string prefix)
        {
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("_"))
                prefix += '_';

            foreach (var column in TableInfo.GetColumns(this.type, this.registry))
                if (column.IsForeignKey) AddIfValidId(this.fkColumns, GetName(column, prefix), column);
                else if (column.IsNative) AddIfValidId(this.columns, GetName(column, prefix), column);
                else this.childrens.Add(new MappedObject(this.registry, this.reader, column.Type, prefix + column.Name) { parentColumn = column, parent = this });
        }

        private void AddIfValidId(Dictionary<int, ColumnInfo> columns, string name, ColumnInfo column)
        {
            int index = this.reader.GetIndexOf(name);
            if (index >= 0)
                columns.Add(index, column);
        }

        private static string GetName(ColumnInfo column, string prefix)
        {
            return column.AutoGenerated && prefix.Length != 0 ? prefix + column.Name : column.Name;
        }

        public object NewObject()
        {
            this.instance = Activator.CreateInstance(this.type);

            foreach (var children in this.childrens)
                children.parentColumn.SetRaw(this.parent.Instance, children.NewObject());

            return this.instance;
        }

        public void ReadFromReader(TableReader reader)
        {
            for (int i = 0; i < this.reader.FieldCount; i++)
                this.SetValue(i, this.reader[i], reader);
        }

        private bool SetValue(int index, object value, TableReader reader)
        {
            if (this.columns.TryGetValue(index, out ColumnInfo column))
            {
                column.Set(this.instance, value);
                this.EnqueueFk(index, value, reader);
                return true;
            }

            foreach (var children in this.childrens)
                if (children.SetValue(index, value, reader))
                    return true;

            return false;
        }

        private void EnqueueFk(int index, object value, TableReader reader)
        {
            if (this.fkColumns.TryGetValue(index, out ColumnInfo column))
                reader.EnqueueForeign(this.instance, value, column);
        }
    }
}
