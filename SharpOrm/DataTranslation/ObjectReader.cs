using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SharpOrm.DataTranslation
{
    public class ObjectReader
    {
        #region Fields
        private string[] props =
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

        public ObjectReader(TableInfo table)
        {
            this.table = table;
        }

        public ObjectReader ContainsProps(params string[] props)
        {
            this.needContains = true;
            return this.SetProps(props);
        }

        public ObjectReader IgnoreProps(params string[] props)
        {
            this.needContains = false;
            return this.SetProps(props);
        }

        private ObjectReader SetProps(string[] props)
        {
            this.props = props ??
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
            return this.props.Length == 0 ||
                this.props.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)) == this.needContains;
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
