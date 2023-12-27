using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections.Generic;

namespace SharpOrm
{
    /// <summary>
    /// Represents a single cell (column and value) of a database table.
    /// </summary>
    public class Cell : ICloneable, IEquatable<Cell>
    {
        /// <summary>
        /// The name of the column.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The value of the cell.
        /// </summary>
        public object Value { get; }

        internal string PropName { get; }

        internal Cell(string name, object value, string propName) : this(name, value)
        {
            this.PropName = propName;
        }

        /// <summary>
        /// Creates a new instance of a cell with the specified column name and value.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value of the cell.</param>
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
            return string.Format("Cell ({0}: {1})", this.Name, ValueToString(this.Value));
        }

        private static string ValueToString(object value)
        {
            if (value is null || value is DBNull)
                return "NULL";

            if (value is DateTime || value is DateTimeOffset || value is TimeSpan)
                value = value.ToString();

            if (value is string str)
                return $"\"{str.Replace("\"", "\\\"")}\"";

            return value?.ToString();
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
            return Convert.ToInt32(cell.Value);
        }

        public static explicit operator float(Cell cell)
        {
            return Convert.ToSingle(cell.Value);
        }

        public static explicit operator decimal(Cell cell)
        {
            return Convert.ToDecimal(cell.Value);
        }

        public static explicit operator bool(Cell cell)
        {
            return Convert.ToBoolean(cell.Value);
        }

        public static explicit operator int?(Cell cell)
        {
            if (cell.Value is null || cell.Value is DBNull)
                return null;

            return Convert.ToInt32(cell.Value);
        }

        public static explicit operator float?(Cell cell)
        {
            if (cell.Value is null || cell.Value is DBNull)
                return null;

            return Convert.ToSingle(cell.Value);
        }

        public static explicit operator decimal?(Cell cell)
        {
            if (cell.Value is null || cell.Value is DBNull)
                return null;

            return Convert.ToDecimal(cell.Value);
        }

        public static explicit operator bool?(Cell cell)
        {
            if (cell.Value is null || cell.Value is DBNull)
                return null;

           return Convert.ToBoolean(cell.Value);
        }

        #endregion
    }
}
