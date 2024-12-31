using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.SqlMethods.Mappers.Sqlite
{
    internal class SqliteDateMethods : SqlMethodCaller
    {
        private readonly Dictionary<string, string> dateFormat = new Dictionary<string, string>
        {
            { "yyyy", "%Y" },
            { "yy", "%y" },
            { "MM", "%m" },
            { "M", "%m" },
            { "dd", "%d" },
            { "d", "%d" },
            { "HH", "%H" },
            { "H", "%H" },
            { "hh", "%I" },
            { "h", "%I" },
            { "mm", "%M" },
            { "ss", "%S" },
            { "fff", "%f" },
            { "tt", "%p" },
            { "dddd", "%W" },
            { "ddd", "%a" },
            { "MMMM", "%B" },
            { "MMM", "%b" }
        };

        public override bool CanWork(SqlMemberInfo member)
        {
            return TranslationUtils.IsDateOrTime(member.DeclaringType) && new[]
                {
                    nameof(DateTime.ToString)
                }.ContainsIgnoreCase(member.Name);
        }

        protected override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression expression, SqlMethodInfo method)
        {
            switch (method.Name)
            {
                case nameof(DateTime.ToString):
                    return new SqlExpression("strftime(?, ?)", GetSqliteFormat(method.Args.FirstOrDefault()?.ToString() ?? SqlMethodMapperUtils.GetDefaultDateOrTimeFormat(method)), expression);
                default: throw new NotSupportedException();
            }
        }

        private string GetSqliteFormat(string format)
        {
            return StringUtils.ReplaceAll(format, dateFormat.ToArray());
        }
    }
}