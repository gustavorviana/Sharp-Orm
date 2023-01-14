using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm
{
    public class Row : IReadOnlyList<Cell>, IEquatable<Row>
    {
        private readonly Cell[] cells;
        private readonly string[] names;

        public Cell this[int index] => this.cells[index];

        public Cell[] Cells => this.cells;

        public string[] ColumnNames => this.names;

        public object this[string columnName]
        {
            get
            {
                if (!(this.GetCell(columnName) is Cell cell))
                    throw new ArgumentException($"A coluna \"{columnName}\" não foi encontrada.");

                return cell.Value;
            }
        }

        public Row()
        {

        }

        public Row(Cell[] cells)
        {
            this.names = cells.Select(column => column.Name).ToArray();
            this.cells = cells;
        }

        public bool Has(string column)
        {
            return this.GetCell(column) != null;
        }

        public Cell GetCell(string column)
        {
            column = column.ToUpper();
            foreach (var cell in this.Cells)
                if (cell.Name.ToUpper() == column)
                    return cell;

            return null;
        }

        public int Count => cells.Length;

        public Cell[] GetOrdenedCells()
        {
            return this.cells.OrderBy(c => c.Name).ToArray();
        }

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
