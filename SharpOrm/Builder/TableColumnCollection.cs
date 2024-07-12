using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a collection of data columns for a table schema.
    /// </summary>
    public class TableColumnCollection : ICollection<DataColumn>
    {
        private readonly List<DataColumn> columns;
        private readonly List<DataColumn> primaryKeys = new List<DataColumn>();

        /// <summary>
        /// Gets the primary key columns.
        /// </summary>
        public DataColumn[] PrimaryKeys => this.primaryKeys.ToArray();

        /// <summary>
        /// Initializes a new instance of the <see cref="TableColumnCollection"/> class.
        /// </summary>
        public TableColumnCollection()
        {
            this.columns = new List<DataColumn>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableColumnCollection"/> class with the specified columns.
        /// </summary>
        /// <param name="columns">The columns to initialize the collection with.</param>
        public TableColumnCollection(params DataColumn[] columns)
        {
            this.columns = new List<DataColumn>(columns);
        }

        /// <summary>
        /// Gets the number of columns in the collection.
        /// </summary>
        public int Count => this.columns.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly { get; }

        /// <summary>
        /// Gets the column at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the column to get.</param>
        /// <returns>The column at the specified index.</returns>
        public DataColumn this[int index] => this.columns[index];

        /// <summary>
        /// Sets the specified column as a primary key.
        /// </summary>
        /// <param name="name">The name of the column to set as primary key.</param>
        /// <returns>The current instance of <see cref="TableColumnCollection"/>.</returns>
        public TableColumnCollection SetPk(string name)
        {
            return this.SetPk(this.IndexOf(name));
        }

        /// <summary>
        /// Sets the column at the specified index as a primary key.
        /// </summary>
        /// <param name="index">The index of the column to set as primary key.</param>
        /// <returns>The current instance of <see cref="TableColumnCollection"/>.</returns>
        public TableColumnCollection SetPk(int index)
        {
            var column = this.columns[index];
            if (!IsPk(column))
                primaryKeys.Add(column);

            return this;
        }

        /// <summary>
        /// Determines whether the specified column name is a primary key.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>True if the column is a primary key; otherwise, false.</returns>
        public bool IsPk(string name)
        {
            return this.IsPk(this.columns.FirstOrDefault(x => x.ColumnName == name));
        }

        /// <summary>
        /// Determines whether the specified column is a primary key.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns>True if the column is a primary key; otherwise, false.</returns>
        public bool IsPk(DataColumn column)
        {
            return this.primaryKeys.Contains(column);
        }

        /// <summary>
        /// Adds columns to the collection, excluding the specified columns.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="registry">The translation registry.</param>
        /// <param name="calls">The columns to exclude.</param>
        public void AddColumnsExcept<T>(TranslationRegistry registry, params Expression<ColumnExpression<T>>[] calls)
        {
            this.AddRange(new TableInfo(typeof(T), registry).GetColumns(calls, true).Select(MapColumn));
        }

        /// <summary>
        /// Adds columns to the collection.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="registry">The translation registry.</param>
        /// <param name="calls">The columns to include.</param>
        public void AddColumns<T>(TranslationRegistry registry, params Expression<ColumnExpression<T>>[] calls)
        {
            this.AddRange(new TableInfo(typeof(T), registry).GetColumns(calls, false).Select(MapColumn));
        }

        /// <summary>
        /// Adds columns to the collection.
        /// </summary>
        /// <param name="columns">The columns to add.</param>
        public void AddColumns(ColumnInfo[] columns)
        {
            this.AddRange(columns.Select(MapColumn));

            foreach (var pkCol in columns.Where(x => x.Key))
                this.SetPk(pkCol.Name);
        }

        /// <summary>
        /// Adds columns to the collection.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="registry">The translation registry.</param>
        public void AddColumns<T>(TranslationRegistry registry)
        {
            var cols = registry.GetTable(typeof(T)).Columns;
            this.AddRange(cols.Select(MapColumn));

            foreach (var pkCol in cols.Where(x => x.Key))
                this.SetPk(pkCol.Name);
        }

        /// <summary>
        /// Adds a primary key column to the collection.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <returns>The added primary key column.</returns>
        public DataColumn AddPk(string columnName)
        {
            return this.AddPk(columnName, typeof(int));
        }

        /// <summary>
        /// Adds a primary key column to the collection.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="type">The data type of the column.</param>
        /// <returns>The added primary key column.</returns>
        public DataColumn AddPk(string columnName, Type type)
        {
            var column = this.Add(columnName, type);
            column.AllowDBNull = false;
            this.primaryKeys.Add(column);
            return column;
        }

        /// <summary>
        /// Adds a column to the collection.
        /// </summary>
        /// <typeparam name="T">The data type of the column.</typeparam>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="maxLength">The maximum length of the column.</param>
        /// <returns>The added column.</returns>
        public DataColumn Add<T>(string columnName, int maxLength = -1)
        {
            return this.Add(columnName, typeof(T), maxLength);
        }

        /// <summary>
        /// Adds a column to the collection.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="type">The data type of the column.</param>
        /// <param name="maxLength">The maximum length of the column.</param>
        /// <returns>The added column.</returns>
        public DataColumn Add(string columnName, Type type, int maxLength = -1)
        {
            var column = new DataColumn(columnName, type) { MaxLength = maxLength };
            this.columns.Add(column);
            return column;
        }

        /// <summary>
        /// Maps a <see cref="ColumnInfo"/> to a <see cref="DataColumn"/>.
        /// </summary>
        /// <param name="item">The column information.</param>
        /// <returns>The mapped data column.</returns>
        private static DataColumn MapColumn(ColumnInfo item)
        {
            return new DataColumn(item.Name, item.Type)
            {
                AllowDBNull = !item.Validations.Any(x => x is RequiredAttribute) && !item.Key
            };
        }

        /// <summary>
        /// Adds a primary key column to the collection.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <returns>The added primary key column.</returns>
        public DataColumn AddPrimaryKey(string columnName)
        {
            return this.AddPrimaryKey(columnName, typeof(int));
        }

        /// <summary>
        /// Adds a primary key column to the collection.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="type">The data type of the column.</param>
        /// <returns>The added primary key column.</returns>
        public DataColumn AddPrimaryKey(string columnName, Type type)
        {
            var column = this.Add(columnName, type);
            this.primaryKeys.Add(column);

            return column;
        }

        /// <summary>
        /// Adds a column to the collection.
        /// </summary>
        /// <param name="item">The column to add.</param>
        public void Add(DataColumn item)
        {
            this.columns.Add(item);
        }

        /// <summary>
        /// Adds a range of columns to the collection.
        /// </summary>
        /// <param name="columns">The columns to add.</param>
        public void AddRange(IEnumerable<DataColumn> columns)
        {
            this.columns.AddRange(columns);
        }

        /// <summary>
        /// Clears the collection of columns and primary keys.
        /// </summary>
        public void Clear()
        {
            this.columns.Clear();
            this.primaryKeys.Clear();
        }

        /// <summary>
        /// Determines whether the collection contains a column with the specified name.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>True if the collection contains a column with the specified name; otherwise, false.</returns>
        public bool Contains(string name)
        {
            return this.columns.Any(x => x.ColumnName == name);
        }

        /// <summary>
        /// Gets the index of the specified column.
        /// </summary>
        /// <param name="item">The column.</param>
        /// <returns>The index of the column.</returns>
        public int IndexOf(DataColumn item)
        {
            return this.columns.IndexOf(item);
        }

        /// <summary>
        /// Gets the index of the column with the specified name.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <returns>The index of the column, or -1 if the column is not found.</returns>
        public int IndexOf(string columnName)
        {
            for (int i = 0; i < this.columns.Count; i++)
                if (this.columns[i].ColumnName == columnName)
                    return i;

            return -1;
        }

        /// <summary>
        /// Removes the specified column from the collection.
        /// </summary>
        /// <param name="item">The column to remove.</param>
        /// <returns>True if the column was removed; otherwise, false.</returns>
        public bool Remove(DataColumn item)
        {
            this.primaryKeys.Remove(item);
            return this.columns.Remove(item);
        }

        /// <summary>
        /// Removes the column with the specified name from the collection.
        /// </summary>
        /// <param name="name">The name of the column to remove.</param>
        /// <returns>True if the column was removed; otherwise, false.</returns>
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

        /// <summary>
        /// Removes the column at the specified index from the collection.
        /// </summary>
        /// <param name="index">The index of the column to remove.</param>
        public void RemoveAt(int index)
        {
            this.columns.RemoveAt(index);
        }

        /// <summary>
        /// Determines whether the collection contains the specified column.
        /// </summary>
        /// <param name="item">The column to check.</param>
        /// <returns>True if the collection contains the column; otherwise, false.</returns>
        public bool Contains(DataColumn item)
        {
            return this.columns.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at the specified array index.
        /// </summary>
        /// <param name="array">The array to copy the elements to.</param>
        /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
        public void CopyTo(DataColumn[] array, int arrayIndex)
        {
            this.columns.CopyTo(array, arrayIndex);
        }

        public IEnumerator<DataColumn> GetEnumerator() => this.columns.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.columns.GetEnumerator();
    }
}
