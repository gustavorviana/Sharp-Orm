using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Table.Constraints
{
    public class CheckConstraint : Constraint, IEquatable<CheckConstraint>
    {
        /// <summary>
        /// Gets the check expression.
        /// </summary>
        public string Expression { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckConstraint"/> class.
        /// </summary>
        /// <param name="tableName">The name of the table that contains the check constraint.</param>
        /// <param name="expression">The check expression.</param>
        /// <param name="name">The name of the check constraint.</param>
        public CheckConstraint(string tableName, string expression, string name = null)
            : base(tableName, name)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Check expression cannot be null or empty.", nameof(expression));

            Expression = expression;
            if (string.IsNullOrEmpty(name))
                Name = GetDefaultName();
        }

        /// <summary>
        /// Generates a default constraint name based on the check constraint information.
        /// Format: CHK_{TableName}_{Hash}
        /// </summary>
        public override string GetDefaultName()
        {
            var hash = Math.Abs(Expression.GetHashCode()).ToString();
            return $"CHK_{Table}_{hash}";
        }

        public override string ToString()
        {
            return $"CHECK ({Expression})";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CheckConstraint);
        }

        public bool Equals(CheckConstraint other)
        {
            return !(other is null) &&
                   base.Equals(other) &&
                   Name == other.Name &&
                   Table == other.Table &&
                   Expression == other.Expression;
        }

        public override int GetHashCode()
        {
            int hashCode = 1666271011;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Table);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Expression);
            return hashCode;
        }

        public static bool operator ==(CheckConstraint left, CheckConstraint right)
        {
            return EqualityComparer<CheckConstraint>.Default.Equals(left, right);
        }

        public static bool operator !=(CheckConstraint left, CheckConstraint right)
        {
            return !(left == right);
        }
    }
}
