using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections;
using System.Text;

namespace SharpOrm.Builder
{
    internal class ParamWriter
    {
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
            if (value is ISqlExpressible expression)
                value = expression.ToSafeExpression(this.grammar.Info.ToReadOnly(), allowAlias);

            if (!(value is SqlExpression exp))
                return this.LoadParameter(value, allowAlias);

            return new StringBuilder()
                .AppendReplaced(
                    exp.ToString(),
                    '?',
                    c => this.LoadParameter(exp.Parameters[c - 1], allowAlias)
                ).ToString();
        }

        private string LoadParameter(object value, bool allowAlias)
        {
            if (this.grammar.Info.Config.EscapeStrings && value is string strVal)
                return SqlExtension.EscapeString(strVal);

            value = TableReaderBase.Registry.ToSql(value);
            if (!(value is byte[]) && value is ICollection)
                throw new NotSupportedException();

            if (QueryConstructor.ToQueryValue(value) is string sql)
                return sql;

            this.writes++;
            return this.grammar.RegisterParameter(string.Format("@{0}{1}", this.paramChar, this.writes), value).ParameterName;
        }

        public void Reset()
        {
            this.writes = 0;
        }
    }
}
