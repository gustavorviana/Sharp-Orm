using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace SharpOrm.Builder.Grammars.Table.Constraints
{
    /// <summary>
    /// Represents foreign key information for a database column.
    /// </summary>
    public class ForeignKeyConstraint : Constraint, IEquatable<ForeignKeyConstraint>
    {
        /// <summary>
        /// Gets the name of the foreign key constraint.
        /// </summary>
        public string ConstraintName { get; }

        /// <summary>
        /// Gets the name of the table that contains the foreign key column.
        /// </summary>
        public string ForeignTable { get; }

        /// <summary>
        /// Gets the name of the foreign key column.
        /// </summary>
        public string ForeignKeyColumn { get; }

        /// <summary>
        /// Gets the name of the referenced table.
        /// </summary>
        public string ReferencedTable { get; }

        /// <summary>
        /// Gets the name of the referenced column.
        /// </summary>
        public string ReferencedColumn { get; }

        /// <summary>
        /// Gets the ON DELETE action for the foreign key.
        /// </summary>
        public DbRule OnDelete { get; set; } = DbRule.None;

        /// <summary>
        /// Gets the ON UPDATE action for the foreign key.
        /// </summary>
        public DbRule OnUpdate { get; set; } = DbRule.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignKeyConstraint"/> class.
        /// </summary>
        /// <param name="foreignTable">The name of the table that contains the foreign key.</param>
        /// <param name="foreignKeyColumn">The name of the foreign key column.</param>
        /// <param name="referencedTable">The name of the referenced table.</param>
        /// <param name="referencedColumn">The name of the referenced column. Defaults to "Id".</param>
        /// <param name="constraintName">The name of the foreign key constraint.</param>
        /// <param name="onDelete">The ON DELETE action. Defaults to null (no action).</param>
        /// <param name="onUpdate">The ON UPDATE action. Defaults to null (no action).</param>
        public ForeignKeyConstraint(
            string foreignTable,
            string foreignKeyColumn,
            string referencedTable,
            string referencedColumn = "Id",
            string constraintName = null)
            : base(referencedTable, constraintName)
        {
            if (string.IsNullOrEmpty(foreignTable))
                throw new ArgumentException("Foreign table name cannot be null or empty.", nameof(foreignTable));

            if (string.IsNullOrEmpty(foreignKeyColumn))
                throw new ArgumentException("Foreign key column name cannot be null or empty.", nameof(foreignKeyColumn));

            if (string.IsNullOrEmpty(referencedColumn))
                throw new ArgumentException("Referenced column name cannot be null or empty.", nameof(referencedColumn));

            ConstraintName = constraintName;
            ForeignTable = foreignTable;
            ForeignKeyColumn = foreignKeyColumn;
            ReferencedTable = referencedTable;
            ReferencedColumn = referencedColumn;
        }

        public override string GetDefaultName()
        {
            return $"FK_{Table}_{ForeignKeyColumn}";
        }

        public override string ToString()
        {
            return $"{ForeignTable}.{ForeignKeyColumn} -> {ReferencedTable}.{ReferencedColumn}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ForeignKeyConstraint);
        }

        public bool Equals(ForeignKeyConstraint other)
        {
            return !(other is null) &&
                   ConstraintName == other.ConstraintName &&
                   ForeignTable == other.ForeignTable &&
                   ForeignKeyColumn == other.ForeignKeyColumn &&
                   ReferencedTable == other.ReferencedTable &&
                   ReferencedColumn == other.ReferencedColumn &&
                   OnDelete == other.OnDelete &&
                   OnUpdate == other.OnUpdate;
        }

        public override int GetHashCode()
        {
            int hashCode = -1937130432;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ConstraintName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ForeignTable);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ForeignKeyColumn);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ReferencedTable);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ReferencedColumn);
            hashCode = hashCode * -1521134295 + EqualityComparer<DbRule>.Default.GetHashCode(OnDelete);
            hashCode = hashCode * -1521134295 + EqualityComparer<DbRule>.Default.GetHashCode(OnUpdate);
            return hashCode;
        }

        public static bool operator ==(ForeignKeyConstraint left, ForeignKeyConstraint right)
        {
            return EqualityComparer<ForeignKeyConstraint>.Default.Equals(left, right);
        }

        public static bool operator !=(ForeignKeyConstraint left, ForeignKeyConstraint right)
        {
            return !(left == right);
        }
    }
}