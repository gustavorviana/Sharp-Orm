using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Table.Constraints
{
    public class PrimaryKeyConstraint : Constraint, IEquatable<PrimaryKeyConstraint>
    {
        /// <summary>
        /// Gets the columns that make up the primary key.
        /// </summary>
        public string[] Columns { get; }

        public bool? IsClustered { get; set; }

        public bool AutoIncrement { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimaryKeyConstraint"/> class.
        /// </summary>
        /// <param name="tableName">The name of the table that contains the primary key.</param>
        /// <param name="columns">The columns that make up the primary key.</param>
        /// <param name="name">The name of the primary key constraint.</param>
        public PrimaryKeyConstraint(string tableName, string[] columns, string name = null) : base(tableName, name)
        {
            if (columns == null || columns.Length == 0)
                throw new ArgumentException("Primary key must have at least one column.", nameof(columns));

            Columns = columns;
        }

        public override string GetDefaultName()
        {
            return $"PK_{Table}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PrimaryKeyConstraint);
        }

        public bool Equals(PrimaryKeyConstraint other)
        {
            return !(other is null) &&
                   base.Equals(other) &&
                   Name == other.Name &&
                   Table == other.Table &&
                   EqualityComparer<string[]>.Default.Equals(Columns, other.Columns);
        }

        public override int GetHashCode()
        {
            int hashCode = 1642396148;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Table);
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(Columns);
            return hashCode;
        }

        public static bool operator ==(PrimaryKeyConstraint left, PrimaryKeyConstraint right)
        {
            return EqualityComparer<PrimaryKeyConstraint>.Default.Equals(left, right);
        }

        public static bool operator !=(PrimaryKeyConstraint left, PrimaryKeyConstraint right)
        {
            return !(left == right);
        }
    }
}
