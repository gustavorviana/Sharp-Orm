using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm.SqlMethods.Mappers.SqlServer
{
    internal class SqlServerDateMethods : SqlMethodCaller
    {
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
                    return new SqlExpression("FORMAT(?,?)", expression, method.Args.FirstOrDefault()?.ToString() ?? SqlMethodMapperUtils.GetDefaultDateOrTimeFormat(method));
                default: throw new NotSupportedException();
            }
        }
    }
}
