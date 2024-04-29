using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SharpOrm.Builder
{
    public class TableColumnCollection : ICollection<DataColumn>
    {
        private readonly List<DataColumn> columns = new List<DataColumn>();

        public int Count => this.columns.Count;

        public bool IsReadOnly { get; } = false;

        public DataColumn this[int index] => this.columns[index];

        public void AddColumnsBy<T>(TranslationRegistry registry)
        {
            this.AddRange(TableInfo.GetColumns(typeof(T), registry).Select(MapColumn));
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
                AllowDBNull = !item.Required,
                Unique = item.Key
            };
        }

        public DataColumn AddUnique(string columnName)
        {
            return this.AddUnique(columnName, typeof(int));
        }

        public DataColumn AddUnique(string columnName, Type type)
        {
            var column = this.Add(columnName, type);

            column.Unique = true;
            column.AutoIncrement = true;
            column.AutoIncrementSeed = 1;
            column.AutoIncrementStep = 1;
            column.AllowDBNull = false;

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
            return this.columns.Remove(item);
        }

        public bool Remove(string name)
        {
            int index = this.IndexOf(name);
            if (index < 0)
                return false;

            this.RemoveAt(index);
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
