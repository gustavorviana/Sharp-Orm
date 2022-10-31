using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm
{
    public class SqlExpression : IEquatable<SqlExpression>
    {
        protected string value;

        protected SqlExpression()
        {

        }

        public SqlExpression(string value)
        {
            this.value = value;
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
                   value == other.value;
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
