using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Table.Constraints
{
    /// <summary>
    /// Base class for all database constraints.
    /// </summary>
    public abstract class Constraint : IEquatable<Constraint>
    {
        /// <summary>
        /// Gets the name of the constraint.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the table name that owns this constraint.
        /// </summary>
        public string Table { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Constraint"/> class.
        /// </summary>
        /// <param name="name">The name of the constraint.</param>
        /// <param name="tableName">The name of the table that owns this constraint.</param>
        protected Constraint(string tableName, string name = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

            Name = name;
            Table = tableName;
        }

        /// <summary>
        /// Gets the effective constraint name (custom or default).
        /// </summary>
        public virtual string GetEffectiveName()
        {
            return string.IsNullOrEmpty(Name) ? GetDefaultName() : Name;
        }

        /// <summary>
        /// Generates a default constraint name.
        /// </summary>
        /// <returns>A default constraint name.</returns>
        public abstract string GetDefaultName();

        public override bool Equals(object obj)
        {
            return Equals(obj as Constraint);
        }

        public bool Equals(Constraint other)
        {
            return !(other is null) &&
                   Name == other.Name &&
                   Table == other.Table;
        }

        public override int GetHashCode()
        {
            int hashCode = -204693524;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Table);
            return hashCode;
        }

        public static bool operator ==(Constraint left, Constraint right)
        {
            return EqualityComparer<Constraint>.Default.Equals(left, right);
        }

        public static bool operator !=(Constraint left, Constraint right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return GetEffectiveName();
        }
    }
}
