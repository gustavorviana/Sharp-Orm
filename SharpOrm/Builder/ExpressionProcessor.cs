using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace SharpOrm.Builder
{
    public class ExpressionProcessor
    {
        private readonly Expression expression;

        private ExpressionProcessor(Expression expression)
        {
            this.expression = expression;
        }

        public static PropInfo[] ParseNewExpression<T>(Expression<Func<T, object>> expression)
        {
            return new ExpressionProcessor(expression.Body).ParseNewExpression();
        }

        public static PropInfo ParseExpression<T>(Expression<Func<T, object>> expression)
        {
            return new ExpressionProcessor(expression.Body).LoadPropInfo();
        }

        private PropInfo[] ParseNewExpression()
        {
            var newExpression = this.GetNewExpression();
            var memberNames = GetMemberNames(newExpression);
            var propInfos = new PropInfo[memberNames.Length];

            for (int i = 0; i < memberNames.Length; i++)
                propInfos[i] = Process(newExpression.Arguments[i], memberNames[i]);

            return propInfos;
        }

        private NewExpression GetNewExpression()
        {
            if (this.expression is NewExpression newExp) return newExp;
            throw new ArgumentException();
        }

        private static string[] GetMemberNames(NewExpression expression)
        {
            return expression.Members.Select(x => x.Name).ToArray();
        }

        public PropInfo LoadPropInfo()
        {
            return this.Process(this.expression);
        }

        private PropInfo Process(Expression expression, string memberName = null)
        {
            if (expression is MemberExpression memberExpression)
                return new PropInfo(GetFullPath(memberExpression), memberName);

            if (expression is MethodCallExpression methodCallExpression)
                return new PropInfo(GetFullPath(methodCallExpression), memberName);

            throw new ArgumentException();
        }

        private List<MemberInfo> GetFullPath(Expression expression)
        {
            List<MemberInfo> methods = new List<MemberInfo>();
            while (expression is MethodCallExpression methodCallExpression)
            {
                methods.Insert(0, methodCallExpression.Method);
                expression = methodCallExpression.Object;
            }

            if (!(expression is MemberExpression memberExpression))
                throw new InvalidOperationException();

            var list = GetFullPath(memberExpression);
            list.AddRange(methods);
            return list;
        }

        private List<MemberInfo> GetFullPath(MemberExpression memberExp)
        {
            var path = new List<MemberInfo>();

            while (memberExp != null)
            {
                path.Insert(0, memberExp.Member);
                memberExp = memberExp.Expression as MemberExpression;
            }

            return path;
        }
    }
}
