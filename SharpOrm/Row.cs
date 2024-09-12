using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm
{
    /// <summary>
    /// Represents a row in a database table.
    /// </summary>
    /// <remarks>
    /// A row is a collection of cells, which represent values in each column of a table.
    /// </remarks>
    public sealed class Row : IReadOnlyList<Cell>, IEquatable<Row>
    {
        private readonly Cell[] cells;
        private readonly string[] names;

        /// <summary>
        /// Gets or sets the cell at the specified index.
        /// </summary>
        /// <param name="index">The index of the cell to get or set.</param>
        /// <returns>The cell at the specified index.</returns>
        public Cell this[int index] => this.cells[index];

        /// <summary>
        /// Gets an array of all cells in the row.
        /// </summary>
        /// <returns>An array of all cells in the row.</returns>
        public Cell[] Cells => this.cells;

        /// <summary>
        /// A read-only collection of cells representing a single row of data.
        /// </summary>
        /// <remarks>
        /// Provides read-only indexed access to the cells of the row, as well as the ability to check for the existence of a specific column and to retrieve a cell by column name. Also provides an ordered array of column names and a string representation of the row.
        /// </remarks>
        public string[] ColumnNames => this.names;

        /// <summary>
        /// Indexer that allows access to a row's cell by column name.
        /// </summary>
        /// <param name="columnName">The name of the column to retrieve the cell from.</param>
        /// <returns>The value of the cell in the specified column.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified column name is not found in the row.</exception>
        public object this[string columnName]
        {
            get
            {
                if (!(this.GetCell(columnName) is Cell cell))
                    throw new ArgumentException($"The column \"{columnName}\" was not found.");

                return cell.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the Row class.
        /// </summary>
        public Row()
        {

        }

        /// <summary>
        /// Initializes a new instance of the Row class with the specified cells.
        /// </summary>
        /// <param name="cells">The cells in the row.</param>
        public Row(params Cell[] cells)
        {
            this.names = cells.Select(column => column.Name).ToArray();
            this.cells = cells;
        }

        /// <summary>
        /// Parses an object into a <see cref="Row"/>.
        /// </summary>
        /// <param name="obj">The object to parse.</param>
        /// <param name="readPk">Whether to read the primary key. Default is true.</param>
        /// <param name="readFk">Whether to read the foreign key. Default is false.</param>
        /// <returns>A <see cref="Row"/> representing the parsed object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the object is null or <see cref="DBNull"/>.</exception>
        public static Row Parse(object obj, bool readPk = true, bool readFk = false, TranslationRegistry registry = null)
        {
            return Parse(obj, obj.GetType(), readPk, readFk);
        }

        /// <summary>
        /// Parses an object into a <see cref="Row"/> with a specified type.
        /// </summary>
        /// <param name="obj">The object to parse.</param>
        /// <param name="type">The type of the object.</param>
        /// <param name="readPk">Whether to read the primary key. Default is true.</param>
        /// <param name="readFk">Whether to read the foreign key. Default is false.</param>
        /// <returns>A <see cref="Row"/> representing the parsed object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the object is null or <see cref="DBNull"/>.</exception>
        public static Row Parse(object obj, Type type, bool readPk = true, bool readFk = false, TranslationRegistry registry = null)
        {
            if (obj is null || obj is DBNull) throw new ArgumentNullException(nameof(obj));
            if (obj is Row row) return row;

            if (registry == null) registry = TranslationRegistry.Default;
            return new Row(registry.GetTable(type).GetObjCells(obj, readPk, readFk).ToArray());
        }

        /// <summary>
        /// Determines whether the row contains a cell with the specified column name.
        /// </summary>
        /// <param name="column">The name of the column.</param>
        /// <returns>True if the row contains a cell with the specified column name; otherwise, false.</returns>
        public bool Has(string column)
        {
            return this.GetCell(column) != null;
        }

        /// <summary>
        /// Gets the cell with the specified column name.
        /// </summary>
        /// <param name="column">The name of the column.</param>
        /// <returns>The cell with the specified column name.</returns>
        public Cell GetCell(string column)
        {
            foreach (var cell in this.Cells)
                if (cell.Name.Equals(column, StringComparison.OrdinalIgnoreCase))
                    return cell;

            return null;
        }

        /// <summary>
        /// Gets the number of cells in the row.
        /// </summary>
        public int Count => cells.Length;

        /// <summary>
        /// Gets an array of cells in the row, sorted by column name.
        /// </summary>
        /// <returns>An array of cells in the row, sorted by column name.</returns>
        public Cell[] GetOrdenedCells()
        {
            return this.cells.OrderBy(c => c.Name).ToArray();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of cells.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection of cells.</returns>
        public IEnumerator<Cell> GetEnumerator() => cells.AsEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => cells.GetEnumerator();

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder("Row");

            foreach (var cell in this.cells)
                builder.AppendFormat(" ({0}: {1})", cell.Name, cell.Value);

            return builder.ToString();
        }

        #region IEquatable

        public override bool Equals(object obj)
        {
            return Equals(obj as Row);
        }

        public bool Equals(Row other)
        {
            return other != null &&
                   EqualityComparer<Cell[]>.Default.Equals(cells, other.cells) &&
                   EqualityComparer<string[]>.Default.Equals(names, other.names) &&
                   EqualityComparer<Cell[]>.Default.Equals(Cells, other.Cells) &&
                   EqualityComparer<string[]>.Default.Equals(ColumnNames, other.ColumnNames) &&
                   Count == other.Count;
        }

        public override int GetHashCode()
        {
            int hashCode = 1707470412;
            hashCode = hashCode * -1521134295 + EqualityComparer<Cell[]>.Default.GetHashCode(cells);
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(names);
            hashCode = hashCode * -1521134295 + EqualityComparer<Cell[]>.Default.GetHashCode(Cells);
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(ColumnNames);
            hashCode = hashCode * -1521134295 + Count.GetHashCode();
            return hashCode;
        }

        #endregion
    }
}
