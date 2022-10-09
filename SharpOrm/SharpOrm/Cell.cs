using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpOrm
{
    public class Cell : ICloneable, IEquatable<Cell>
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

        public override string ToString()
        {
            return string.Format("Column ({0}: {1})", this.Name, this.Value);
        }

        #region IEquatable

        public override bool Equals(object obj)
        {
            return Equals(obj as Cell);
        }

        public bool Equals(Cell other)
        {
            return other != null &&
                   Name == other.Name &&
                   EqualityComparer<object>.Default.Equals(Value, other.Value);
        }

        public override int GetHashCode()
        {
            int hashCode = -244751520;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(Value);
            return hashCode;
        }

        #endregion

        #region Explicit operator

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

        #endregion
    }
}
