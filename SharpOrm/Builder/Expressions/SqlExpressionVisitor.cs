using SharpOrm.DataTranslation;
using SharpOrm.ForeignKey;
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

        private readonly TranslationRegistry _registry;
        private readonly ExpressionConfig _config;
        private readonly IReadonlyQueryInfo _info;
        internal readonly IForeignKeyNode _parent;
        private readonly Type _rootType;

        public SqlExpressionVisitor(Type rootType, TranslationRegistry registry, IReadonlyQueryInfo info, ExpressionConfig config, IForeignKeyNode parent)
        {
            _registry = registry ?? TranslationRegistry.Default;
            _rootType = rootType;
            _parent = parent;
            _config = config;
            _info = info;
        }

        public SqlMember Visit(Expression expression, string memberName = null)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            expression = UnwrapUnaryExpression(expression);

            if (expression is NewExpression)
                throw new NotSupportedException(Messages.Expressions.NewExpressionDisabled);

            var path = GetPath(expression);

            if (path.IsStaticProperty())
                return new SqlMember(path.TargetMember, memberName);

            return new SqlMember(path.Path.ToArray(), path.TargetMember, path.Childs.ToArray(), GetColumnName(path.TargetMember), memberName);
        }

        private string GetColumnName(SqlMemberInfo member)
        {
            return _registry.GetTable(member.DeclaringType).GetColumn(member.Member)?.Name ?? member.Member.Name;
        }

        private Expression UnwrapUnaryExpression(Expression expression)
        {
            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null && unaryExpression.Operand is MemberExpression)
                return unaryExpression.Operand;

            return expression;
        }

        private MemberPath GetPath(Expression expression)
        {
            if (expression is MemberExpression memberExpression)
                return VisitMemberExpression(memberExpression);

            if (expression is MethodCallExpression methodCallExpression)
                return VisitMethodCall(methodCallExpression);

            throw new NotSupportedException(string.Format("Expression type {0} is not supported", expression.GetType().Name));
        }

        private MemberPath VisitMemberExpression(MemberExpression memberExp)
        {
            ValidateSubmembers(memberExp);

            var info = new MemberPath();
            var currentExp = memberExp;

            while (currentExp != null)
            {
                info.AddMember(currentExp);
                currentExp = currentExp.Expression as MemberExpression;
            }

            return info.LoadRootMember();
        }

        private void ValidateSubmembers(MemberExpression memberExp)
        {
            if (!_config.HasFlag(ExpressionConfig.SubMembers) && memberExp.Expression as MemberExpression != null)
                throw new NotSupportedException(Messages.Expressions.SubmembersDisabled);
        }

        private MemberPath VisitMethodCall(MethodCallExpression methodCallExp)
        {
            if (!_config.HasFlag(ExpressionConfig.Method))
                throw new NotSupportedException(Messages.Expressions.FunctionDisabled);

            Expression currentExp = methodCallExp;
            var methods = new List<SqlMemberInfo>();

            while (currentExp is MethodCallExpression)
            {
                var currentMethodCall = (MethodCallExpression)currentExp;
                methods.Insert(0, CreateMethodInfo(currentMethodCall));

                if (currentMethodCall.Object == null)
                    break;

                currentExp = currentMethodCall.Object;
            }

            return ProcessMethodCallResult(currentExp, methods);
        }

        private SqlMethodInfo CreateMethodInfo(MethodCallExpression methodCall)
        {
            var arguments = methodCall.Arguments.Select(x => VisitMethodArgument(x)).ToArray();
            return new SqlMethodInfo(
                methodCall.Method.IsStatic ?
                methodCall.Method.DeclaringType :
                methodCall.Object.Type,
                methodCall.Method,
                arguments
            );
        }

        private MemberPath ProcessMethodCallResult(Expression currentExp, List<SqlMemberInfo> methods)
        {
            var finalMethodCall = currentExp as MethodCallExpression;
            if (finalMethodCall != null)
            {
                var memberType = finalMethodCall.Type.IsAssignableFrom(_rootType) ? _rootType : finalMethodCall.Type.DeclaringType;

                var path = new MemberPath();

                path.AddMembers(methods);
                path.TargetMember = new SqlMethodInfo(finalMethodCall.Type, finalMethodCall.Method, new object[0]);
                return path;
            }

            var memberExpression = currentExp as MemberExpression;
            if (memberExpression != null)
            {
                var path = VisitMemberExpression(memberExpression);
                path.Childs.AddRange(methods);
                return path;
            }

            throw new InvalidOperationException(Messages.Expressions.Invalid);
        }

        private object VisitMethodArgument(Expression expression)
        {
            if (TryGetConstantValue(expression, out var value))
                return value;

            expression = UnwrapUnaryExpression(expression);

            if (!(expression is MemberExpression memberExp))
                throw new NotSupportedException(string.Format(Messages.Expressions.ArgumentNotSupported, expression.GetType().Name));

            return GetMethodArgValue(memberExp);
        }

        private object GetMethodArgValue(MemberExpression memberExp)
        {
            var target = GetTarget(memberExp.Expression);
            if (target == null && !ReflectionUtils.IsStatic(memberExp.Member))
                return GetMethodColumn(memberExp);

            if (ReflectionUtils.TryGetValue(memberExp.Member, target, out var value))
                return value;

            throw new NotSupportedException(string.Format(Messages.Expressions.MemberNotSupported, memberExp.Member.GetType().Name));
        }

        private object GetMethodColumn(MemberExpression memberExp)
        {
            if (memberExp.Expression.GetType().ToString() != PropertyExpressionTypeName)
                return new MemberInfoColumn(memberExp.Member);

            return _info.Config.Methods.ApplyMember(_info, Visit(memberExp, null), _parent);
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
            throw new InvalidOperationException(string.Format(Messages.Expressions.LoadIncompatible, mType, member.Name));
        }
    }
}