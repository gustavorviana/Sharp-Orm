using SharpOrm.DataTranslation;
using SharpOrm.SqlMethods;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace SharpOrm.Builder.Expressions
{
    internal class ExpressionProcessor<T>
    {
        private readonly IReadonlyQueryInfo info;
        private readonly ExpressionConfig config;
        private readonly SqlExpressionVisitor visitor;

        public ExpressionProcessor(IReadonlyQueryInfo info, ExpressionConfig config)
        {
            this.info = info;
            this.config = config;
            this.visitor = new SqlExpressionVisitor(typeof(T), info, config);
        }

        public IEnumerable<Column> ParseColumns(Expression<ColumnExpression<T>> expression)
        {
            foreach (var item in ParseExpression(expression))
                yield return new ExpressionColumn(item.Name, info.Config.Methods.ApplyMember(info, ProcessMemberInfo(item), out var isFk), isFk || item.Member.MemberType == MemberTypes.Method)
                {
                    Alias = item.Alias ?? (item.Childs.Length > 0 ? item.Name : null)
                };
        }

        private SqlMember ProcessMemberInfo(SqlMember info)
        {
            if (info.Childs.Length > 1 && info.Childs.Last().Name == nameof(object.ToString))
            {
                var toCheck = info.Childs[info.Childs.Length - 2];
                bool isDateOrTime = TranslationUtils.IsDateOrTime(toCheck.DeclaringType);
                bool isTimeOfDay = toCheck.Member.Name == nameof(DateTime.TimeOfDay);

                info.Childs = isDateOrTime && isTimeOfDay
                    ? info.Childs.Where((x, index) => index != info.Childs.Length - 2).ToArray()
                    : info.Childs;
            }

            return info;
        }

        public IEnumerable<SqlMember> ParseExpression(Expression<ColumnExpression<T>> expression)
        {
            if (expression.Body is NewExpression newExpression)
            {
                if (!config.HasFlag(ExpressionConfig.New))
                    throw new NotSupportedException(Messages.Expressions.NewExpressionDisabled);

                for (int i = 0; i < newExpression.Members.Count; i++)
                    yield return visitor.Visit(
                        newExpression.Arguments[i],
                        newExpression.Members[i].Name
                    );
            }
            else
            {
                yield return visitor.Visit(expression.Body);
            }
        }
    }
}