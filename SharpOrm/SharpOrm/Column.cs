using SharpOrm.Builder;
using System;
using System.Collections.Generic;

namespace SharpOrm
{
    public class Column : SqlExpression, IEquatable<Column>, IEquatable<string>
    {
        public static Column All => new Column { value = "*" };

        protected Column() : base("")
        {

        }

        public Column(string name) : base(name.RemoveInvalidNameChars())
        {

        }

        public Column(string name, string alias) : base(null)
        {
            this.value = $"{name.RemoveInvalidNameChars()} AS {alias.AlphaNumericOnly()}";
        }

        public Column(SqlExpression expression, string alias) : base(null)
        {
            this.value = $"{expression} AS {alias.AlphaNumericOnly()}";
        }

        public static explicit operator Column(string rawColumn)
        {
            return new Column { value = rawColumn };
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Column);
        }

        public bool Equals(Column other)
        {
            return other != null &&
                   value == other.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + EqualityComparer<string>.Default.GetHashCode(value);
        }

        public bool Equals(string other)
        {
            return other != null &&
                   value == other;
        }
    }
}
