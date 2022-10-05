using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm
{
    public class Row : IReadOnlyList<Cell>
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
    }
}
