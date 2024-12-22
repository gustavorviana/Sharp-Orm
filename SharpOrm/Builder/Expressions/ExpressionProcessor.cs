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

        public ExpressionProcessor(IReadonlyQueryInfo info, bool allowSubMembers)
        {
            this.info = info;
            this.allowSubMembers = allowSubMembers;
        }

        private string GetMemberName(MemberExpression memberExp)
        {
            if (memberExp == null) return string.Empty;

            var current = memberExp;
            var parts = new List<string>();

            while (current != null)
            {
                parts.Insert(0, current.Member.Name);
                current = current.Expression as MemberExpression;
            }

            return string.Join(".", parts);
        }

        public IEnumerable<Column> ParseColumns(Expression<ColumnExpression<T>> expression)
        {
            foreach (var item in this.ParseExpression(expression))
                yield return new ExpressionColumn(info.Config.Methods.ApplyMember(info, item))
                {
                    Alias = item.Alias ?? (item.HasChilds ? item.Name : null)
                };
        }

        public IEnumerable<SqlMember> ParseExpression(Expression<ColumnExpression<T>> expression)
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

        internal IEnumerable<SqlMember> ParseNewExpression(Expression<Func<T, object>> expression)
        {
            if (expression.Body is NewExpression newExpression)
                for (int i = 0; i < newExpression.Members.Count; i++)
                    yield return ParseExpression(newExpression.Arguments[i], newExpression.Members[i].Name);
        }

        internal SqlMember ParseColumnExpression(Expression<ColumnExpression<T>> expression)
        {
            return this.ParseExpression(expression.Body);
        }

        private SqlMember ParseExpression(Expression expression, string memberName = null)
        {
            List<SqlMemberInfo> members;
            MemberInfo member;

            if (expression is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression unaryMemberExpression)
                expression = unaryMemberExpression;

            if (expression is MemberExpression memberExpression)
                members = GetFullPath(memberExpression, out member);
            else if (expression is MethodCallExpression methodCallExpression)
                members = GetFullPath(methodCallExpression, out member);
            else
                throw new NotSupportedException();

            if (ReflectionUtils.IsStatic(member) && member is PropertyInfo)
                return new SqlMember(new SqlPropertyInfo(member), memberName);

            return new SqlMember(member, members.ToArray(), memberName);
        }

        private object GetArgument(Expression expression)
        {
            if (expression is MemberExpression memberExpression) return GetMemberValue(memberExpression);
            if (expression is ConstantExpression constantExp) return constantExp.Value;

            throw new NotSupportedException(expression.GetType().FullName);
        }

        private object GetMemberValue(MemberExpression memberExpression)
        {
            return GetExpressionValue(memberExpression, GetTargetFromExpression(memberExpression.Expression, null));
        }

        private object GetTargetFromExpression(Expression expression, object owner)
        {
            if (expression is ConstantExpression constantExpression) return constantExpression.Value;
            return GetExpressionValue(expression, null);
        }

        private object GetExpressionValue(Expression expression, object owner)
        {
            if (!(expression is MemberExpression memberExpression)) return null;

            if (owner == null && !ReflectionUtils.IsStatic(memberExpression.Member))
                return new MemberInfoColumn(memberExpression.Member);

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
                methods.Insert(0, new SqlMethodInfo(methodCallExpression.Method, methodCallExpression.Arguments.Select(GetArgument).ToArray()));

                if (methodCallExpression.Object == null)
                    break;

                expression = methodCallExpression.Object;
            }

            if (expression is MethodCallExpression methodCall)
            {
                member = methodCall.Method;
                return methods;
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
    }
}