using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    internal class ParamWriter
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private readonly Grammar grammar;
        private readonly char paramChar;
        private int writes = 0;

        public ParamWriter(Grammar grammar, char paramChar)
        {
            this.grammar = grammar;
            this.paramChar = paramChar;
        }

        public string LoadValue(object value, bool allowAlias)
        {
            return this.InternalLoadValue(value, allowAlias, false);
        }

        private string InternalLoadValue(object value, bool allowAlias, bool isValueOfList)
        {
            if (value is ISqlExpressible expression)
                value = expression.ToSafeExpression(this.grammar.Info.ToReadOnly(), allowAlias);

            if (!(value is SqlExpression exp))
                return this.LoadParameter(value, allowAlias, isValueOfList);

            return new StringBuilder()
                .AppendReplaced(
                    exp.ToString(),
                    '?',
                    c => this.LoadParameter(exp.Parameters[c - 1], allowAlias, isValueOfList)
                ).ToString();
        }

        private string LoadParameter(object value, bool allowAlias, bool isValueOfList)
        {
            if (value is ICollection col && !this.CanTranslate(value))
                return string.Format("({0})", this.RegisterCollectionParameters(col, allowAlias, isValueOfList));

            value = this.grammar.Registry.ToSql(value);
            if (!(value is byte[]) && value is ICollection)
                throw new NotSupportedException();

            if (ToSql(value) is string sqlValue)
                return sqlValue;

            this.writes++;
            return this.grammar.RegisterParameter(string.Format("@{0}{1}", this.paramChar, this.writes), value).ParameterName;
        }

        private bool CanTranslate(object value)
        {
            return this.grammar.Registry.GetFor(value.GetType()) != null;
        }

        private string RegisterCollectionParameters(ICollection collection, bool allowAlias, bool isValueOfList)
        {
            if (isValueOfList)
                throw new NotSupportedException("You cannot use a collection as a value in another collection.");

            string items = string.Join(", ", collection.Cast<object>().Select(c => this.LoadValue(c, allowAlias)));
            if (string.IsNullOrEmpty(items))
                throw new InvalidOperationException("Cannot use an empty collection in the query.");

            return items;
        }

        public void Reset()
        {
            this.writes = 0;
        }

        private string ToSql(object value)
        {
            if (TranslationUtils.IsNull(value))
                return "NULL";

            if (TranslationUtils.IsNumeric(value))
                return ((IConvertible)value).ToString(Invariant);

            return null;
        }
    }
}
