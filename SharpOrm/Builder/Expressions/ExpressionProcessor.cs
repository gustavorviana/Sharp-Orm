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
        private readonly bool allowSubMembers;
        private readonly SqlExpressionVisitor visitor;

        public ExpressionProcessor(IReadonlyQueryInfo info, bool allowSubMembers)
        {
            this.info = info;
            this.allowSubMembers = allowSubMembers;
            this.visitor = new SqlExpressionVisitor(info, allowSubMembers);
        }

        public IEnumerable<Column> ParseColumns(Expression<ColumnExpression<T>> expression)
        {
            foreach (var item in ParseExpression(expression))
                yield return new ExpressionColumn(info.Config.Methods.ApplyMember(info, item))
                {
                    Alias = item.Alias ?? (item.HasChilds ? item.Name : null)
                };
        }

        public IEnumerable<SqlMember> ParseExpression(Expression<ColumnExpression<T>> expression)
        {
            if (expression.Body is NewExpression newExpression)
            {
                if (!allowSubMembers)
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

        internal IEnumerable<SqlMember> ParseNewExpression(Expression<Func<T, object>> expression)
        {
            if (expression.Body is NewExpression newExpression)
                for (int i = 0; i < newExpression.Members.Count; i++)
                    yield return visitor.Visit(
                        newExpression.Arguments[i],
                        newExpression.Members[i].Name
                    );
        }

        internal SqlMember ParseColumnExpression(Expression<ColumnExpression<T>> expression)
        {
            return visitor.Visit(expression.Body);
        }
    }
}