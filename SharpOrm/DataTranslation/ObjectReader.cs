using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.DataTranslation
{
    public class ObjectReader
    {
        #region Fields
        private string[] columns =
#if NET46_OR_GREATER || NET5_0_OR_GREATER 
                Array.Empty<string>();
#else
                new string[0];
#endif

        private bool needContains;

        private readonly TableInfo table;
        #endregion

        public bool ReadPk { get; set; }
        public bool ReadFk { get; set; }

        public bool Validate { get; set; }

        private readonly bool hasUpdateColumn;
        private readonly bool hasCreateColumn;

        public bool IgnoreTimestamps { get; set; }

        public ObjectReader(TableInfo table)
        {
            this.table = table;
            this.hasUpdateColumn = !string.IsNullOrEmpty(table.Timestamp?.UpdatedAtColumn);
        }

        public ObjectReader Only<T>(params Expression<ColumnExpression<T>>[] calls)
        {
            this.needContains = true;
            return this.SetProps(calls);
        }

        public ObjectReader Only(params string[] columns)
        {
            this.needContains = true;
            return this.SetColumns(columns);
        }

        public ObjectReader Except(params string[] columns)
        {
            this.needContains = false;
            return this.SetColumns(columns);
        }

        public ObjectReader Except<T>(params Expression<ColumnExpression<T>>[] calls)
        {
            this.needContains = false;
            return this.SetProps(calls);
        }

        private ObjectReader SetProps<T>(Expression<ColumnExpression<T>>[] calls)
        {
            return this.SetColumns(calls.Select(ExpressionUtils<T>.GetPropName).ToArray());
        }

        private ObjectReader SetColumns(string[] columns)
        {
            this.columns = columns ??
#if NET5_0_OR_GREATER
            Array.Empty<string>();
#else
                new string[0];
#endif
            return this;
        }

        public Row ReadRow(object owner)
        {
            return new Row(this.ReadCells(owner).ToArray());
        }

        #region ExpandoObject
        private IEnumerable<Cell> ReadDictCells(IDictionary<string, object> owner)
        {
            foreach (var item in owner)
                if (IsAllowedName(item.Key) || (this.IsKey(item.Key) && this.UsePk(item.Value)))
                    yield return new Cell(item.Key, item.Value);
        }

        private bool IsKey(string name)
        {
            return name.Equals("id", StringComparison.OrdinalIgnoreCase);
        }

        private bool UsePk(object value)
        {
            return this.ReadPk && !TranslationUtils.IsInvalidPk(value);
        }
        #endregion

        #region ObjectReader
        public IEnumerable<Cell> ReadCells(object owner)
        {
            if (owner is IDictionary<string, object> dict)
                return this.ReadDictCells(dict);

            return this.ReadObjectCells(owner);
        }

        private IEnumerable<Cell> ReadObjectCells(object owner)
        {
            for (int i = 0; i < table.Columns.Length; i++)
            {
                var cell = this.GetCell(owner, table.Columns[i]);
                if (cell != null) yield return cell;
            }

            if (this.hasCreateColumn)
                yield return new Cell(this.table.Timestamp.UpdatedAtColumn, DateTime.UtcNow);
        }

        private Cell GetCell(object owner, ColumnInfo column)
        {
            if (!this.IsAllowedName(column.Name)) return null;
            if (column.ForeignInfo != null)
            {
                if (this.CanReadFk(column))
                    return new Cell(column.ForeignInfo.ForeignKey, this.GetFkValue(owner, column.GetRaw(owner), column));

                return null;
            }

            object value = ProcessValue(column, owner);
            if (column.Key && !this.CanReadKey(value))
                return null;

            if (this.Validate) column.ValidateValue(value);
            return new Cell(column.Name, value);
        }

        private object ProcessValue(ColumnInfo column, object owner)
        {
            object obj = column.Get(owner);
            if (!this.ReadFk || !column.Type.IsClass || column.ForeignInfo == null || TranslationUtils.IsNull(obj))
                return obj;

            return this.table.registry.GetTable(column.Type).Columns.FirstOrDefault(c => c.Key).Get(obj);
        }

        private object GetFkValue(object owner, object value, ColumnInfo fkColumn)
        {
            var type = GetValidType(fkColumn.Type);
            if (type == typeof(Row))
                return null;

            var table = this.table.registry.GetTable(type);
            var pkColumn = table.Columns.First(c => c.Key);

            if (TranslationUtils.IsInvalidPk(value) || !(fkColumn.GetRaw(owner) is object fkInstance))
                return null;

            return pkColumn.Get(fkInstance);
        }

        private static Type GetValidType(Type type)
        {
            return ReflectionUtils.IsCollection(type) ? ReflectionUtils.GetGenericArg(type) : type;
        }
        #endregion

        protected bool IsAllowedName(string name)
        {
            if (this.IsTimeStamps(name)) return false;

            return this.columns.Length == 0 ||
            this.columns.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)) == this.needContains;
        }

        private bool IsTimeStamps(string name)
        {
            if (this.IgnoreTimestamps) return false;

            return (this.hasUpdateColumn && name.Equals(this.table.Timestamp.UpdatedAtColumn, StringComparison.OrdinalIgnoreCase)) ||
                (this.hasCreateColumn && name.Equals(this.table.Timestamp.CreatedAtColumn, StringComparison.OrdinalIgnoreCase));
        }

        private bool CanReadFk(ColumnInfo column)
        {
            return this.ReadFk && !this.table.Columns.Any(c => c != column && c.Name.Equals(column.ForeignInfo?.ForeignKey, StringComparison.OrdinalIgnoreCase));
        }

        protected bool CanReadKey(object value)
        {
            return this.ReadPk && !TranslationUtils.IsInvalidPk(value);
        }
    }
}
