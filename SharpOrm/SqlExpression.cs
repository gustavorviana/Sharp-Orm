using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm
{
    public class SqlExpression : IEquatable<SqlExpression>
    {
        private readonly string value;
        public object[] Parameters { get; protected set; }

        protected SqlExpression()
        {

        }

        public SqlExpression(string value, params object[] arguments)
        {
            if (value.Count(c => c == '?') != arguments.Length)
                throw new InvalidOperationException("The operation cannot be performed because the arguments passed in the SQL query do not match the provided parameters.");

            this.value = value;
            this.Parameters = arguments;
        }

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
