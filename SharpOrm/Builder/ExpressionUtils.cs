using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.Builder
{
    internal static class ExpressionUtils<T>
    {
        public static Column GetColumn(Expression<ColumnExpression<T>> columnExpression)
        {
            return new MemberInfoColumn(GetColumnMember(columnExpression, out _));
        }

        public static MemberInfo GetColumnMember(Expression<ColumnExpression<T>> columnExpression, out Type rootClass)
        {
            var paths = GetMemberPath(columnExpression, true);
            rootClass = paths.Last().DeclaringType;
            var member = paths.First();
            if (member.IsDefined(typeof(NotMappedAttribute)))
                throw new InvalidOperationException($"It is not possible to retrieve the column {member.DeclaringType.FullName}.{member.Name}, it was defined as unmapped.");

            return member;
        }

        public static string GetPropName(Expression<ColumnExpression<T>> exp)
        {
            return GetMemberExpression(exp).Member.Name;
        }

        internal static IEnumerable<MemberInfo> GetMemberPath(Expression<ColumnExpression<T>> propertyExpression, bool allowNativeType)
        {
            var memberExpression = GetMemberExpression(propertyExpression);

            if (!allowNativeType)
                SqlExpressionVisitor.ValidateMemberType(memberExpression.Member);

            while (memberExpression != null)
            {
                yield return memberExpression.Member;
                memberExpression = memberExpression.Expression as MemberExpression;
            }
        }

        internal static MemberExpression GetMemberExpression(Expression<ColumnExpression<T>> expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (expression.Body is MemberExpression mExp) return mExp;

            if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression mExp2)
                return mExp2;

            throw new ArgumentException("The provided expression is not valid.");
        }
    }
}
