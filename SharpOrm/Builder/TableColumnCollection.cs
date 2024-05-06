using SharpOrm.Builder.DataTranslation;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.Builder
{
    public class TableColumnCollection : ICollection<DataColumn>
    {
        private readonly List<DataColumn> columns;
        private readonly List<DataColumn> primaryKeys = new List<DataColumn>();

        public DataColumn[] PrimaryKeys => this.primaryKeys.ToArray();

        public TableColumnCollection(params DataColumn[] columns)
        {
            this.columns = new List<DataColumn>(columns);
        }

        public int Count => this.columns.Count;

        public bool IsReadOnly { get; } = false;

        public DataColumn this[int index] => this.columns[index];

        public TableColumnCollection SetPk(string name)
        {
            return this.SetPk(this.IndexOf(name));
        } 

        public TableColumnCollection SetPk(int index)
        {
            var column = this.columns[index];
            if (!IsPk(column))
                primaryKeys.Add(column);

            return this;
        }

        public bool IsPk(string name)
        {
            return this.IsPk(this.columns.FirstOrDefault(x => x.ColumnName == name));
        }

        public bool IsPk(DataColumn column)
        {
            return this.primaryKeys.Contains(column);
        }

        public void AddColumnsExcept<T>(TranslationRegistry registry, params Expression<ColumnExpression<T>>[] calls)
        {
            this.AddRange(TableInfo.GetColumns(typeof(T), registry, calls, true).Select(MapColumn));
        }

        public void AddColumns<T>(TranslationRegistry registry, params Expression<ColumnExpression<T>>[] calls)
        {
            this.AddRange(TableInfo.GetColumns(typeof(T), registry, calls, false).Select(MapColumn));
        }

        public void AddColumns<T>(TranslationRegistry registry)
        {
            var cols = TableInfo.GetColumns(typeof(T), registry).ToArray();
            this.AddRange(cols.Select(MapColumn));

            foreach (var pkCol in cols.Where(x => x.Key))
                this.SetPk(pkCol.Name);
        }

        public DataColumn AddPk(string columnName)
        {
            return this.AddPk(columnName, typeof(int));
        }

        public DataColumn AddPk(string columnName, Type type)
        {
            var column = this.Add(columnName, type);
            column.AllowDBNull = false;
            this.primaryKeys.Add(column);
            return column;
        }

        public DataColumn Add<T>(string columnName, int maxLength = -1)
        {
            return this.Add(columnName, typeof(T), maxLength);
        }

        public DataColumn Add(string columnName, Type type, int maxLength = -1)
        {
            var column = new DataColumn(columnName, type) { MaxLength = maxLength };
            this.columns.Add(column);
            return column;
        }

        private static DataColumn MapColumn(ColumnInfo item)
        {
            return new DataColumn(item.Name, item.Type)
            {
                AllowDBNull = !item.Required
            };
        }

        public DataColumn AddPrimaryKey(string columnName)
        {
            return this.AddPrimaryKey(columnName, typeof(int));
        }

        public DataColumn AddPrimaryKey(string columnName, Type type)
        {
            var column = this.Add(columnName, type);
            this.primaryKeys.Add(column);

            return column;
        }

        public void Add(DataColumn item)
        {
            this.columns.Add(item);
        }

        public void AddRange(IEnumerable<DataColumn> columns)
        {
            this.columns.AddRange(columns);
        }

        public void Clear()
        {
            this.columns.Clear();
            this.primaryKeys.Clear();
        }

        public bool Contains(string name)
        {
            return this.columns.Any(x => x.ColumnName == name);
        }

        public int IndexOf(DataColumn item)
        {
            return this.columns.IndexOf(item);
        }

        public int IndexOf(string columnName)
        {
            for (int i = 0; i < this.columns.Count; i++)
                if (this.columns[i].ColumnName == columnName)
                    return i;

            return -1;
        }

        public bool Remove(DataColumn item)
        {
            this.primaryKeys.Remove(item);
            return this.columns.Remove(item);
        }

        public bool Remove(string name)
        {
            int index = this.IndexOf(name);
            if (index < 0)
                return false;

            this.RemoveAt(index);
            if (this.primaryKeys.FirstOrDefault(x => x.ColumnName == name) is DataColumn column)
                this.primaryKeys.Remove(column);

            return true;
        }

        public void RemoveAt(int index)
        {
            this.columns.RemoveAt(index);
        }

        public bool Contains(DataColumn item)
        {
            return this.columns.Contains(item);
        }

        public void CopyTo(DataColumn[] array, int arrayIndex)
        {
            this.columns.CopyTo(array, arrayIndex);
        }

        public IEnumerator<DataColumn> GetEnumerator() => this.columns.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.columns.GetEnumerator();
    }
}
