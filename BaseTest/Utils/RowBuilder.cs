using SharpOrm;
using SharpOrm.DataTranslation;

namespace BaseTest.Utils
{
    internal class RowBuilder
    {
        private readonly List<Cell> _cells = [];

        public RowBuilder AddObject(object obj, bool readPk = true, bool readFk = false, TranslationRegistry? registry = null)
        {
            return AddCells(Row.Parse(obj, obj.GetType(), readPk, readFk, registry).Cells);
        }

        public RowBuilder AddObject(string prefix, object obj, bool readPk = true, bool readFk = false, TranslationRegistry? registry = null)
        {
            return AddCells(prefix, Row.Parse(obj, obj.GetType(), readPk, readFk, registry).Cells);
        }

        public RowBuilder AddCells(string prefix, params Cell[] cells)
        {
            _cells.AddRange(cells.Select(x => new Cell($"{prefix}{x.Name}", x.Value)));
            return this;
        }

        public RowBuilder AddCells(params Cell[] cells)
        {
            _cells.AddRange(cells);
            return this;
        }

        public RowBuilder AddCell(Cell cell, string? prefix = null)
        {
            prefix ??= "";

            return AddCell($"{prefix}{cell.Name}", cell.Value);
        }

        public RowBuilder AddCell(string name, object value)
        {
            _cells.Add(new Cell(name, value));
            return this;
        }

        public Row ToRow()
        {
            return new Row(_cells);
        }
    }
}
