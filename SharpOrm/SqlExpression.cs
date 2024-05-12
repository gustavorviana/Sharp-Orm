using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm
{
    /// <summary>
    /// Represents a SQL values with optional parameters to be used in a SQL query.
    /// </summary>
    public class SqlExpression : IEquatable<SqlExpression>
    {
        private readonly string value;
        /// <summary>
        /// Gets the parameters used in the SQL values.
        /// </summary>
        public object[] Parameters { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the SqlExpression class with an empty SQL values string and no parameters.
        /// </summary>
        protected SqlExpression()
        {

        }

        /// <summary>
        /// Initializes a new instance of the SqlExpression class with the provided SQL values string and parameters.
        /// </summary>
        /// <param name="value">The SQL values string (to signal an argument, use '?').</param>
        /// <param name="parameters">The parameters used in the SQL values.</param>
        public SqlExpression(StringBuilder value, params object[] parameters)
        {
            if (value.Count('?') != parameters.Length)
                throw new InvalidOperationException(Messages.OperationCannotBePerformedArgumentsMismatch);

            this.value = value.ToString();
            this.Parameters = parameters;
        }

        /// <summary>
        /// Initializes a new instance of the SqlExpression class with the provided SQL values string and parameters.
        /// </summary>
        /// <param name="value">The SQL values string (to signal an argument, use '?').</param>
        /// <param name="parameters">The parameters used in the SQL values.</param>
        public SqlExpression(string value, params object[] parameters)
        {
            if (value.Count(c => c == '?') != parameters.Length)
                throw new InvalidOperationException(Messages.OperationCannotBePerformedArgumentsMismatch);

            this.value = value;
            this.Parameters = parameters;
        }

        /// <summary>
        /// Returns the SQL values string.
        /// </summary>
        /// <returns>The SQL values string.</returns>
        public override string ToString()
        {
            return this.value;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SqlExpression);
        }

        public bool Equals(SqlExpression other)
        {
            return other is SqlExpression &&
                   value == other.value &&
                   this.EqualsParams(other);
        }

        private bool EqualsParams(SqlExpression other)
        {
            if (other.Parameters.Length != this.Parameters.Length)
                return false;

            for (int i = 0; i < this.Parameters.Length; i++)
                if (this.Parameters[i] != other.Parameters[i])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            return -1584136870 + EqualityComparer<string>.Default.GetHashCode(value);
        }

        public static explicit operator SqlExpression(StringBuilder builder)
        {
            return new SqlExpression(builder.ToString());
        }

        public static explicit operator SqlExpression(string expression)
        {
            return new SqlExpression(expression);
        }

        public static explicit operator string(SqlExpression expression)
        {
            return expression.ToString();
        }
    }
}
