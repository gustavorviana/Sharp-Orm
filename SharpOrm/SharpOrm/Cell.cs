using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm
{
    public class Cell : ICloneable
    {
        public string Name { get; }

        public object Value { get; }

        public Cell(string name, object value)
        {
            this.Name = name;
            this.Value = value;
        }

        public object Clone()
        {
            return new Cell(this.Name, this.Value);
        }

        public static explicit operator string(Cell cell)
        {
            return cell.Value?.ToString();
        }

        public static explicit operator int(Cell cell)
        {
            return (int)cell.Value;
        }

        public static explicit operator float(Cell cell)
        {
            return (float)cell.Value;
        }

        public static explicit operator decimal(Cell cell)
        {
            return (decimal)cell.Value;
        }

        public static explicit operator bool(Cell cell)
        {
            return (int)cell == 1;
        }
    }
}
