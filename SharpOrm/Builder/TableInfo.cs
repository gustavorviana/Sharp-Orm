﻿using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents information about a database table.
    /// </summary>
    public class TableInfo
    {
        private readonly BindingFlags propertiesFlags = BindingFlags.Instance | BindingFlags.Public;
        public Type Type { get; }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        public string Name { get; }

        public bool HasNonNative { get; private set; }
        public bool HasFk { get; private set; }

        /// <summary>
        /// Gets an array of column information for the table.
        /// </summary>
        public ColumnInfo[] Columns { get; }

        /// <summary>
        /// Initializes a new instance of the TableInfo class with the specified translation configuration and type.
        /// </summary>
        /// <param name="type">The type representing the table.</param>
        public TableInfo(Type type) : this(new TranslationRegistry(), type)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TableInfo class with the specified translation configuration and type.
        /// </summary>
        /// <param name="config">The translation configuration.</param>
        /// <param name="type">The type representing the table.</param>
        public TableInfo(TranslationRegistry config, Type type)
        {
            if (type == null || type.IsAbstract || type == typeof(Row))
                throw new InvalidOperationException($"Invalid type provided for the {nameof(TableInfo)} class.");

            this.Type = type;
            this.Name = GetNameOf(type);
            this.Columns = this.GetColumns(config).ToArray();
        }

        private IEnumerable<ColumnInfo> GetColumns(TranslationRegistry registry)
        {
            foreach (var col in this.GetColumnsFromProperties(registry))
            {
                bool isFk = !string.IsNullOrEmpty(col.ForeignKey);
                if (isFk)
                    this.HasFk = true;

                if (col.IsNative && isFk)
                    this.HasNonNative = true;

                yield return col;
            }

            foreach (var col in this.GetColumnsFromFields(registry))
            {
                bool isFk = !string.IsNullOrEmpty(col.ForeignKey);
                if (isFk)
                    this.HasFk = true;

                if (col.IsNative && isFk)
                    this.HasNonNative = true;

                yield return col;
            }
        }

        private IEnumerable<ColumnInfo> GetColumnsFromProperties(TranslationRegistry registry)
        {
            foreach (var prop in Type.GetProperties(propertiesFlags))
                if (prop.GetCustomAttribute<NotMappedAttribute>() == null)
                    yield return new ColumnInfo(registry, prop);
        }

        private IEnumerable<ColumnInfo> GetColumnsFromFields(TranslationRegistry registry)
        {
            foreach (var field in Type.GetFields(propertiesFlags))
                if (field.GetCustomAttribute<NotMappedAttribute>() == null)
                    yield return new ColumnInfo(registry, field);
        }

        /// <summary>
        /// Gets the cells representing the column values of the specified owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <param name="ignorePrimaryKey">True to ignore the primary key column, false otherwise.</param>
        /// <param name="readForeignKey">If true and there is no column named Foreign Key Attribute.Name then use the primary key defined in the primary key object, otherwise do nothing with the primary key.</param>
        /// <returns>An enumerable of cells.</returns>
        public IEnumerable<Cell> GetCells(object owner, bool ignorePrimaryKey = false, bool readForeignKey = false)
        {
            foreach (var column in this.Columns)
            {
                bool isFk = !string.IsNullOrEmpty(column.ForeignKey);
                if (isFk)
                {
                    if (readForeignKey && CanLoadForeignColumn(column))
                        yield return new Cell(column.ForeignKey, this.GetFkValue(owner, column.GetRaw(owner), column));
                    continue;
                }

                object value = ProcessValue(column, owner, readForeignKey);
                if ((column.Key && (ignorePrimaryKey || TranslationUtils.IsInvalidPk(value))))
                    continue;

                yield return new Cell(column.Name, value);
            }
        }

        private bool CanLoadForeignColumn(ColumnInfo column)
        {
            return !this.Columns.Any(c => c != column && c.Name.Equals(column.ForeignKey, StringComparison.OrdinalIgnoreCase));
        }

        private object ProcessValue(ColumnInfo column, object owner, bool readForeignKey)
        {
            object obj = column.Get(owner);
            if (!readForeignKey || !column.Type.IsClass || string.IsNullOrEmpty(column.ForeignKey) || TranslationUtils.IsNull(obj))
                return obj;

            if (obj is null)
                return null;

            return new TableInfo(column.Type).Columns.FirstOrDefault(c => c.Key).Get(obj);
        }

        private object GetFkValue(object owner, object value, ColumnInfo fkColumn)
        {
            var table = TableReaderBase.GetTable(fkColumn.Type);
            var pkColumn = table.Columns.First(c => c.Key);

            if (TranslationUtils.IsInvalidPk(value) || !(fkColumn.GetRaw(owner) is object fkInstance))
                return null;

            return pkColumn.Get(fkInstance);
        }

        public object CreateInstance()
        {
            return Activator.CreateInstance(this.Type);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Name, this.Type);
        }

        public static string GetNameOf(Type type)
        {
            return type.GetCustomAttribute<TableAttribute>(false)?.Name ?? type.Name;
        }
    }
}
