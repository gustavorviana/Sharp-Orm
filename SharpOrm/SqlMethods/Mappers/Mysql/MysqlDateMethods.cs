using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.SqlMethods.Mappers.Mysql
{
    internal class MysqlDateMethods : SqlMethodCaller
    {
        private readonly Dictionary<string, string> dateFormat = new Dictionary<string, string>
        {
            { "yyyy", "%Y" },
            { "yy", "%y" },
            { "MM", "%m" },
            { "M", "%c" },
            { "dd", "%d" },
            { "d", "%e" },
            { "HH", "%H" },
            { "H", "%k" },
            { "hh", "%h" },
            { "h", "%l" },
            { "mm", "%i" },
            { "ss", "%s" },
            { "fff", "%f" },
            { "tt", "%p" },
            { "dddd", "%W" },
            { "ddd", "%a" },
            { "MMMM", "%M" },
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
                    return new SqlExpression("DATE_FORMAT(?,?)", expression, GetMysqlFormat(method.Args.FirstOrDefault()?.ToString() ?? SqlMethodMapperUtils.GetDefaultDateOrTimeFormat(method)));
                default: throw new NotSupportedException();
            }
        }

        private string GetMysqlFormat(string format)
        {
            return StringUtils.ReplaceAll(format, dateFormat.ToArray());
        }
    }
}
