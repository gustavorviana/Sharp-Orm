using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.Builder.Expressions
{
    internal class SqlExpressionVisitor
    {
        private readonly IReadonlyQueryInfo info;
        private readonly bool allowSubMembers;

        public SqlExpressionVisitor(IReadonlyQueryInfo info, bool allowSubMembers)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            this.info = info;
            this.allowSubMembers = allowSubMembers;
        }

        public SqlMember Visit(Expression expression, string memberName = null)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            expression = UnwrapUnaryExpression(expression);

            List<SqlMemberInfo> members;
            MemberInfo member;
            GetMembersAndMemberInfo(expression, out members, out member);

            if (ReflectionUtils.IsStatic(member) && member is PropertyInfo)
                return new SqlMember(new SqlPropertyInfo(member), memberName);

            return new SqlMember(member, members.ToArray(), memberName);
        }

        private Expression UnwrapUnaryExpression(Expression expression)
        {
            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null && unaryExpression.Operand is MemberExpression)
            {
                return unaryExpression.Operand;
            }
            return expression;
        }

        private void GetMembersAndMemberInfo(Expression expression, out List<SqlMemberInfo> members, out MemberInfo member)
        {
            var memberExpression = expression as MemberExpression;
            if (memberExpression != null)
            {
                members = VisitMemberExpression(memberExpression, out member);
                return;
            }

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null)
            {
                members = VisitMethodCall(methodCallExpression, out member);
                return;
            }

            if (expression is NewExpression)
            {
                throw new NotSupportedException(Messages.Expressions.NewExpressionDisabled);
            }

            throw new NotSupportedException(string.Format("Expression type {0} is not supported", expression.GetType().Name));
        }

        private List<SqlMemberInfo> VisitMemberExpression(MemberExpression memberExp, out MemberInfo member)
        {
            ValidateSubmembers(memberExp);

            var path = GatherMemberPath(memberExp);
            member = path[0];
            path.RemoveAt(0);

            return new List<SqlMemberInfo>(path.Select(x => new SqlPropertyInfo(x)));
        }

        private void ValidateSubmembers(MemberExpression memberExp)
        {
            if (!allowSubMembers && memberExp.Expression as MemberExpression != null)
            {
                throw new NotSupportedException(Messages.Expressions.SubmembersDisabled);
            }
        }

        private List<MemberInfo> GatherMemberPath(MemberExpression memberExp)
        {
            var path = new List<MemberInfo>();
            var currentExp = memberExp;

            while (currentExp != null)
            {
                path.Insert(0, currentExp.Member);
                currentExp = currentExp.Expression as MemberExpression;
            }

            return path;
        }

        private List<SqlMemberInfo> VisitMethodCall(MethodCallExpression methodCallExp, out MemberInfo member)
        {
            if (!allowSubMembers)
                throw new NotSupportedException(Messages.Expressions.FunctionDisabled);

            var methods = new List<SqlMemberInfo>();
            Expression currentExp = methodCallExp;

            while (currentExp is MethodCallExpression)
            {
                var currentMethodCall = (MethodCallExpression)currentExp;
                methods.Insert(0, CreateMethodInfo(currentMethodCall));

                if (currentMethodCall.Object == null)
                    break;

                currentExp = currentMethodCall.Object;
            }

            return ProcessMethodCallResult(currentExp, methods, out member);
        }

        private SqlMethodInfo CreateMethodInfo(MethodCallExpression methodCall)
        {
            var arguments = methodCall.Arguments.Select(VisitArgument).ToArray();
            return new SqlMethodInfo(methodCall.Method, arguments);
        }

        private List<SqlMemberInfo> ProcessMethodCallResult(Expression currentExp, List<SqlMemberInfo> methods, out MemberInfo member)
        {
            var finalMethodCall = currentExp as MethodCallExpression;
            if (finalMethodCall != null)
            {
                member = finalMethodCall.Method;
                return methods;
            }

            var memberExpression = currentExp as MemberExpression;
            if (memberExpression != null)
            {
                var list = VisitMemberExpression(memberExpression, out member);
                list.AddRange(methods);
                return list;
            }

            throw new InvalidOperationException("Invalid expression structure");
        }

        private object VisitArgument(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            var memberExp = expression as MemberExpression;
            if (memberExp != null)
                return GetMemberValue(memberExp);

            var constExp = expression as ConstantExpression;
            if (constExp != null)
                return constExp.Value;

            throw new NotSupportedException(
                string.Format("Argument expression type {0} is not supported", expression.GetType().Name));
        }

        private object GetMemberValue(MemberExpression memberExp)
        {
            var target = GetTarget(memberExp.Expression);

            if (target == null && !ReflectionUtils.IsStatic(memberExp.Member))
                return new MemberInfoColumn(memberExp.Member);

            var fieldInfo = memberExp.Member as FieldInfo;
            if (fieldInfo != null)
                return fieldInfo.GetValue(target);

            var propertyInfo = memberExp.Member as PropertyInfo;
            if (propertyInfo != null)
                return propertyInfo.GetValue(target);

            throw new NotSupportedException(
                string.Format("Member type {0} is not supported", memberExp.Member.GetType().Name));
        }

        private object GetTarget(Expression expression)
        {
            if (expression == null)
                return null;

            var constExp = expression as ConstantExpression;
            if (constExp != null)
                return constExp.Value;

            var memberExp = expression as MemberExpression;
            if (memberExp != null)
                return GetMemberValue(memberExp);

            return null;
        }
    }
}