using SharpOrm.DataTranslation;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.Builder.Expressions
{
    internal class SqlExpressionVisitor
    {
        private const string PropertyExpressionTypeName = "System.Linq.Expressions.PropertyExpression";

        private readonly ExpressionConfig config;
        private readonly IReadonlyQueryInfo info;
        private readonly Type rootType;

        public bool ForceTablePrefix { get; set; }

        public SqlExpressionVisitor(Type rootType, IReadonlyQueryInfo info, ExpressionConfig config)
        {
            this.rootType = rootType;
            this.config = config;
            this.info = info;
        }

        public SqlMember Visit(Expression expression, string memberName = null)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            expression = UnwrapUnaryExpression(expression);

            if (expression is NewExpression)
                throw new NotSupportedException(Messages.Expressions.NewExpressionDisabled);

            var members = GetMembers(expression, out var member);

            if (ReflectionUtils.IsStatic(member) && member is PropertyInfo)
                return new SqlMember(new SqlPropertyInfo(expression.Type, member), memberName);

            var memberType = member.DeclaringType.IsAssignableFrom(rootType) ? rootType : member.DeclaringType;

            return new SqlMember(memberType, member, members.ToArray(), memberName);
        }

        private Expression UnwrapUnaryExpression(Expression expression)
        {
            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null && unaryExpression.Operand is MemberExpression)
                return unaryExpression.Operand;

            return expression;
        }

        private List<SqlMemberInfo> GetMembers(Expression expression, out MemberInfo member)
        {
            if (expression is MemberExpression memberExpression)
                return VisitMemberExpression(memberExpression, out member);

            if (expression is MethodCallExpression methodCallExpression)
                return VisitMethodCall(methodCallExpression, out member);

            throw new NotSupportedException(string.Format("Expression type {0} is not supported", expression.GetType().Name));
        }

        private List<SqlMemberInfo> VisitMemberExpression(MemberExpression memberExp, out MemberInfo member)
        {
            ValidateSubmembers(memberExp);

            var path = GatherMemberPath(memberExp);
            member = path[0];
            path.RemoveAt(0);

            return new List<SqlMemberInfo>(path.Select(x => new SqlPropertyInfo(memberExp.Expression.Type, x)));
        }

        private void ValidateSubmembers(MemberExpression memberExp)
        {
            if (!config.HasFlag(ExpressionConfig.SubMembers) && memberExp.Expression as MemberExpression != null)
                throw new NotSupportedException(Messages.Expressions.SubmembersDisabled);
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
            if (!config.HasFlag(ExpressionConfig.Method))
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
            var arguments = methodCall.Arguments.Select(VisitMethodArgument).ToArray();
            return new SqlMethodInfo(
                methodCall.Method.IsStatic ?
                methodCall.Method.DeclaringType :
                methodCall.Object.Type,
                methodCall.Method,
                arguments
            );
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

        private object VisitMethodArgument(Expression expression)
        {
            if (TryGetConstantValue(expression, out var value))
                return value;

            expression = UnwrapUnaryExpression(expression);

            if (!(expression is MemberExpression memberExp))
                throw new NotSupportedException(
                    string.Format("Argument expression type {0} is not supported", expression.GetType().Name));

            return GetMethodArgValue(memberExp);
        }

        private object GetMethodArgValue(MemberExpression memberExp)
        {
            var target = GetTarget(memberExp.Expression);
            if (target == null && !ReflectionUtils.IsStatic(memberExp.Member))
                return GetMethodColumn(memberExp);

            if (ReflectionUtils.TryGetValue(memberExp.Member, target, out var value))
                return value;

            throw new NotSupportedException(
                string.Format("Member type {0} is not supported", memberExp.Member.GetType().Name));
        }

        private object GetMethodColumn(MemberExpression memberExp)
        {
            if (memberExp.Expression.GetType().ToString() != PropertyExpressionTypeName)
                return new MemberInfoColumn(memberExp.Member);

            return this.info.Config.Methods.ApplyMember(this.info, Visit(memberExp), ForceTablePrefix);
        }

        private object GetTarget(Expression expression)
        {
            if (TryGetConstantValue(expression, out var value))
                return value;

            if (expression is MemberExpression memberExp &&
                ReflectionUtils.IsStatic(memberExp.Member) &&
                ReflectionUtils.TryGetValue(memberExp.Member, null, out value))
                return value;

            return null;
        }

        private static bool TryGetConstantValue(Expression expression, out object value)
        {
            value = null;
            if (expression is ConstantExpression consExp)
            {
                value = consExp.Value;
                return true;
            }

            return false;
        }

        internal static IEnumerable<MemberInfo> GetMemberPath(MemberExpression memberExpression, bool allowNativeType)
        {
            if (!allowNativeType)
                ValidateMemberType(memberExpression.Member);

            while (memberExpression != null)
            {
                yield return memberExpression.Member;
                memberExpression = memberExpression.Expression as MemberExpression;
            }
        }

        internal static void ValidateMemberType(MemberInfo member)
        {
            if (!TranslationUtils.IsNative(ReflectionUtils.GetMemberType(member), false))
                return;

            string mType = member.MemberType == MemberTypes.Property ? "property" : "field";
            throw new InvalidOperationException($"It's not possible to load the {mType} '{member.Name}' because its type is incompatible.");
        }
    }
}