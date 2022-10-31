using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm
{
    public class Column : IExpressionConversion, IEquatable<Column>, IEquatable<string>
    {
        #region Fields\Properties
        private readonly SqlExpression expression;

        public string Name { get; protected set; }
        public string Alias { get; protected set; }

        public static Column All => new Column(new SqlExpression("*"));
        public static Column CountAll => new Column(new SqlExpression("COUNT(*)"));
        #endregion

        protected Column()
        {

        }

        public Column(string name, string alias) : this(name)
        {
            this.Alias = alias;
        }

        public Column(string name)
        {
            this.Name = name;
        }

        public Column(SqlExpression expression)
        {
            this.expression = expression;
        }

        public virtual SqlExpression ToExpression(QueryBase query)
        {
            if (this.expression != null)
                return this.expression;

            StringBuilder builder = new StringBuilder();
            if (!string.IsNullOrEmpty(query.Info.Alias))
                builder.AppendFormat("{0}.", query.Info.Config.ApplyNomenclatureableOfColumnAliasConfig(query.Info.Alias));

            builder.Append(query.Info.Config.ApplyNomenclatureableOfColumnAliasConfig(this.Name));

            if (!string.IsNullOrEmpty(this.Alias))
                builder.AppendFormat(" AS {0}", this.Alias);

            return (SqlExpression)builder;
        }

        public static explicit operator Column(string rawColumn)
        {
            return new Column(new SqlExpression(rawColumn));
        }

        #region IEquatable

        public override bool Equals(object obj)
        {
            return Equals(obj as Column);
        }

        public bool Equals(Column other)
        {
            return other != null &&
                   Name == other.Name &&
                   Alias == other.Alias &&
                   expression.Equals(other.expression);
        }

        public override int GetHashCode()
        {
            return -1584136870 + EqualityComparer<string>.Default.GetHashCode(Name);
        }

        public bool Equals(string other)
        {
            return other != null &&
                   Name == other;
        }

        #endregion
    }
}
