using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

        internal IEnumerable<string> ParseColumnNames(Expression<ColumnExpression<T>> expression)
        {
            return ParseExpression(expression).Select(x => x.Name);
        }

        public IEnumerable<Column> ParseColumns(Expression<ColumnExpression<T>> expression)
        {
            return ParseExpression(expression).Select(BuildColumn);
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

        public ExpressionColumn ParseColumn<R>(Expression<ColumnExpression<T, R>> expression)
        {
            return BuildColumn(ParseExpressionField<R>(expression));
        }

        private ExpressionColumn BuildColumn(SqlMember member)
        {
            var sqlExpression = info.Config.Methods.ApplyMember(info, ProcessMemberInfo(member), out var isFk);
            return new ExpressionColumn(member.Name, sqlExpression, isFk || member.Member.MemberType == MemberTypes.Method)
            {
                Alias = member.Alias ?? (member.Childs.Length > 0 ? member.Name : null)
            };
        }

        public SqlMember ParseExpressionField<R>(Expression<ColumnExpression<T, R>> expression)
        {
            if (!(expression.Body is NewExpression newExpression))
                return visitor.Visit(expression.Body);

            if (!config.HasFlag(ExpressionConfig.New))
                throw new NotSupportedException(Messages.Expressions.NewExpressionDisabled);

            return visitor.Visit(
               newExpression.Arguments.First(),
               newExpression.Members.First().Name
           );
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