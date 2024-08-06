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
            var member = GetValueMemberPath(columnExpression).First();
            if (member.IsDefined(typeof(NotMappedAttribute)))
                throw new InvalidOperationException($"It is not possible to retrieve the column {member.DeclaringType.FullName}.{member.Name}, it was defined as unmapped.");

            return new MemberInfoColumn(member);
        }

        public static List<MemberInfoColumn> GetColumnPath(Expression<ColumnExpression<T>> propertyExpression)
        {
            var cols = new List<MemberInfoColumn>();

            foreach (var member in GetValueMemberPath(propertyExpression))
            {
                ValidateMemberType(member);
                cols.Insert(0, new MemberInfoColumn(member));
            }

            return cols;
        }

        private static void ValidateMemberType(MemberInfo member)
        {
            if (!TranslationUtils.IsNative(ReflectionUtils.GetMemberValueType(member), false))
                return;

            string mType = member.MemberType == MemberTypes.Property ? "property" : "field";
            throw new InvalidOperationException($"It's not possible to load the {mType} '{member.Name}' because its type is incompatible.");
        }

        public static string GetPropName(Expression<ColumnExpression<T>> exp)
        {
            return GetMemberExpression(exp).Member.Name;
        }

        private static IEnumerable<MemberInfo> GetValueMemberPath(Expression<ColumnExpression<T>> propertyExpression)
        {
            var memberExpression = GetMemberExpression(propertyExpression);

            while (memberExpression != null)
            {
                yield return memberExpression.Member;
                memberExpression = memberExpression.Expression as MemberExpression;
            }
        }

        private static MemberExpression GetMemberExpression(Expression<ColumnExpression<T>> expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (expression.Body is MemberExpression mExp) return mExp;

            if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression mExp2)
                return mExp2;

            throw new ArgumentException("The provided expression is not valid.");
        }
    }
}
