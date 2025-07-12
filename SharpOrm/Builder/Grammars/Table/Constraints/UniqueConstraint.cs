using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Table.Constraints
{
    public class UniqueConstraint : Constraint, IEquatable<UniqueConstraint>
    {
        /// <summary>
        /// Gets the columns that make up the unique constraint.
        /// </summary>
        public string[] Columns { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UniqueConstraint"/> class.
        /// </summary>
        /// <param name="tableName">The name of the table that contains the unique constraint.</param>
        /// <param name="columns">The columns that make up the unique constraint.</param>
        /// <param name="name">The name of the unique constraint.</param>
        public UniqueConstraint(string tableName, string[] columns, string name = null)
            : base(name, tableName)
        {
            if (columns == null || columns.Length == 0)
                throw new ArgumentException("Unique constraint must have at least one column.", nameof(columns));

            Columns = columns;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UniqueConstraint"/> class with a single column.
        /// </summary>
        /// <param name="tableName">The name of the table that contains the unique constraint.</param>
        /// <param name="column">The column that makes up the unique constraint.</param>
        /// <param name="name">The name of the unique constraint.</param>
        public UniqueConstraint(string tableName, string column, string name = null)
            : this(tableName, new[] { column }, name)
        {
        }

        /// <summary>
        /// Generates a default constraint name based on the unique constraint information.
        /// Format: UC_{TableName}_{Column1}_{Column2}...
        /// </summary>
        public override string GetDefaultName()
        {
            return $"UC_{Table}_{string.Join("_", Columns)}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as UniqueConstraint);
        }

        public bool Equals(UniqueConstraint other)
        {
            return !(other is null) &&
                   base.Equals(other) &&
                   Name == other.Name &&
                   Table == other.Table &&
                   EqualityComparer<string[]>.Default.Equals(Columns, other.Columns);
        }

        public override int GetHashCode()
        {
            int hashCode = -1366775272;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Table);
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(Columns);
            return hashCode;
        }

        /// <summary>
        /// Indicates whether this is a composite unique constraint (multiple columns).
        /// </summary>
        public bool IsComposite => Columns.Length > 1;

        public static bool operator ==(UniqueConstraint left, UniqueConstraint right)
        {
            return EqualityComparer<UniqueConstraint>.Default.Equals(left, right);
        }

        public static bool operator !=(UniqueConstraint left, UniqueConstraint right)
        {
            return !(left == right);
        }
    }
}
