using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm
{
    /// <summary>
    /// Represents a SQL expression with optional parameters to be used in a SQL query.
    /// </summary>
    public class SqlExpression : IEquatable<SqlExpression>
    {
        private readonly string value;
        /// <summary>
        /// Gets the parameters used in the SQL expression.
        /// </summary>
        public object[] Parameters { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the SqlExpression class with an empty SQL expression string and no parameters.
        /// </summary>
        protected SqlExpression()
        {

        }

        /// <summary>
        /// Initializes a new instance of the SqlExpression class with the provided SQL expression string and parameters.
        /// </summary>
        /// <param name="value">The SQL expression string.</param>
        /// <param name="arguments">The parameters used in the SQL expression.</param>
        public SqlExpression(string value, params object[] arguments)
        {
            if (value.Count(c => c == '?') != arguments.Length)
                throw new InvalidOperationException("The operation cannot be performed because the arguments passed in the SQL query do not match the provided parameters.");

            this.value = value;
            this.Parameters = arguments;
        }

        /// <summary>
        /// Returns the SQL expression string.
        /// </summary>
        /// <returns>The SQL expression string.</returns>
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
            return other != null &&
                   value == other.value &&
                   Parameters == other.Parameters;
        }

        public override int GetHashCode()
        {
            return -1584136870 + EqualityComparer<string>.Default.GetHashCode(value);
        }

        public static bool operator ==(SqlExpression a, SqlExpression b) => a is SqlExpression && a.Equals(b);

        public static bool operator !=(SqlExpression a, SqlExpression b) => a is SqlExpression && !a.Equals(b);

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
