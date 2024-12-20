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
    public class ExpressionProcessor
    {
        private readonly bool allowSubMembers;

        public ExpressionProcessor(bool allowSubMembers)
        {
            this.allowSubMembers = allowSubMembers;
        }

        public IEnumerable<Column> ParseColumns<T>(IReadonlyQueryInfo info, Expression<ColumnExpression<T>> expression)
        {
            foreach (var item in this.ParseExpression(expression))
                yield return new ExpressionColumn(info.Config.Methods.ApplyMember(info, item))
                {
                    Alias = item.Alias ?? (item.HasChilds ? item.Name : null)
                };
        }

        public IEnumerable<SqlProperty> ParseExpression<T>(Expression<ColumnExpression<T>> expression)
        {
            if (expression.Body is NewExpression newExpression)
            {
                if (!allowSubMembers) throw new NotSupportedException(Messages.Expressions.NewExpressionDisabled);

                for (int i = 0; i < newExpression.Members.Count; i++)
                    yield return ParseExpression(newExpression.Arguments[i], newExpression.Members[i].Name);
            }
            else
            {
                yield return this.ParseExpression(expression.Body);
            }
        }

        internal IEnumerable<SqlProperty> ParseNewExpression<T>(Expression<Func<T, object>> expression)
        {
            if (expression.Body is NewExpression newExpression)
                for (int i = 0; i < newExpression.Members.Count; i++)
                    yield return ParseExpression(newExpression.Arguments[i], newExpression.Members[i].Name);
        }

        internal SqlProperty ParseColumnExpression<T>(Expression<ColumnExpression<T>> expression)
        {
            return this.ParseExpression(expression.Body);
        }

        private SqlProperty ParseExpression(Expression expression, string memberName = null)
        {
            List<SqlMemberInfo> members;
            MemberInfo member;

            if (expression is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression unaryMemberExpression)
                expression = unaryMemberExpression;

            if (expression is MemberExpression memberExpression) members = GetFullPath(memberExpression, out member);
            else if (expression is MethodCallExpression methodCallExpression) members = GetFullPath(methodCallExpression, out member);
            else throw new NotSupportedException();

            if (IsStatic(member) && member is PropertyInfo)
                return new SqlProperty(new SqlPropertyInfo(member), memberName);

            return new SqlProperty(member, members.ToArray(), memberName);
        }

        private static object GetArgument(Expression expression)
        {
            if (expression is MemberExpression memberExpression) return GetMemberValue(memberExpression);
            if (expression is ConstantExpression constantExp) return constantExp.Value;

            throw new NotSupportedException(expression.GetType().FullName);
        }

        private static object GetMemberValue(MemberExpression memberExpression)
        {
            return GetExpressionValue(memberExpression, GetTargetFromExpression(memberExpression.Expression, null));
        }

        private static object GetTargetFromExpression(Expression expression, object owner)
        {
            if (expression is ConstantExpression constantExpression) return constantExpression.Value;

            return GetExpressionValue(expression, null);
        }

        private static object GetExpressionValue(Expression expression, object owner)
        {
            if (!(expression is MemberExpression memberExpression)) return null;

            var member = memberExpression.Member;
            if (member is FieldInfo fieldInfo) return fieldInfo.GetValue(owner);
            if (member is PropertyInfo propertyInfo) return propertyInfo.GetValue(owner);

            throw new NotSupportedException($"The member of type \"{member.GetType().FullName}\" is not supported.");
        }

        private List<SqlMemberInfo> GetFullPath(Expression expression, out MemberInfo member)
        {
            if (!allowSubMembers) throw new NotSupportedException(Messages.Expressions.FunctionDisabled);

            List<SqlMemberInfo> methods = new List<SqlMemberInfo>();
            while (expression is MethodCallExpression methodCallExpression)
            {
                if (methodCallExpression.Method.IsStatic)
                    throw new NotSupportedException("Static methods is not suported.");

                methods.Insert(0, new SqlMethodInfo(methodCallExpression.Method, methodCallExpression.Arguments.Select(GetArgument).ToArray()));
                expression = methodCallExpression.Object;
            }

            if (!(expression is MemberExpression memberExpression))
                throw new InvalidOperationException();

            var list = GetFullPath(memberExpression, out member);
            list.AddRange(methods);
            return list;
        }

        private List<SqlMemberInfo> GetFullPath(MemberExpression memberExp, out MemberInfo member)
        {
            if (!allowSubMembers && memberExp.Expression as MemberExpression != null)
                throw new NotSupportedException(Messages.Expressions.SubmembersDisabled);

            var path = new List<MemberInfo>();

            while (memberExp != null)
            {
                path.Insert(0, memberExp.Member);
                memberExp = memberExp.Expression as MemberExpression;
            }

            member = path.First();
            path.RemoveAt(0);

            return new List<SqlMemberInfo>(path.Select(x => new SqlPropertyInfo(x)));
        }

        private static bool IsStatic(MemberInfo member)
        {
            if (member is PropertyInfo propertyInfo)
                return propertyInfo.GetMethod?.IsStatic ?? false;

            if (member is MethodInfo methodInfo)
                return methodInfo.IsStatic;

            return false;
        }
    }
}
