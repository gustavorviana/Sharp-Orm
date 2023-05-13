using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace SharpOrm.Builder
{
    internal class SqlLambdaVisitor : ExpressionVisitor
    {
        #region Fields
        private static readonly Dictionary<ExpressionType, string> ExpressionMap = new Dictionary<ExpressionType, string>()
        {
            { ExpressionType.Equal, " = " },
            { ExpressionType.NotEqual, " != " },
            { ExpressionType.GreaterThan, " > " },
            { ExpressionType.GreaterThanOrEqual, " >= " },
            { ExpressionType.LessThan, " < " },
            { ExpressionType.LessThanOrEqual, " <= " },
            { ExpressionType.AndAlso, " AND " },
            { ExpressionType.OrElse, " OR " },
            { ExpressionType.Add, " + " },
            { ExpressionType.Subtract, " - " },
            { ExpressionType.Multiply, " * " },
            { ExpressionType.Divide, " / " },
            { ExpressionType.Modulo, " % " },
            { ExpressionType.And, " & " },
            { ExpressionType.Or, " | " },
            { ExpressionType.ExclusiveOr, " ^ " },
            { ExpressionType.LeftShift, " << " },
            { ExpressionType.RightShift, " >> " }
        };

        private readonly QueryInfo _queryInfo;
        private readonly StringBuilder sql = new StringBuilder();
        private readonly List<object> args = new List<object>();

        public static SqlExpression ParseLambda(QueryInfo info, Expression expression)
        {
            var visitor = new SqlLambdaVisitor(info);
            visitor.Visit(expression);
            return new SqlExpression(visitor.sql.ToString(), visitor.args.ToArray());
        }
        #endregion

        private SqlLambdaVisitor(QueryInfo queryInfo)
        {
            _queryInfo = queryInfo;
        }

        public override Expression Visit(Expression node)
        {
            if (node == null)
                return null;

            if (node.NodeType == ExpressionType.Constant)
                return this.VisitConstant((ConstantExpression)node);

            if (node.NodeType == ExpressionType.MemberAccess && node is MemberExpression member && member.Expression != null)
                return this.OnMemberAccess(member);

            return base.Visit(node);
        }

        private Expression OnMemberAccess(MemberExpression member)
        {
            if (member?.GetType().FullName == "System.Linq.Expressions.FieldExpression")
                return this.VisitField(member);

            if (member.Expression is ConstantExpression exp)
                return this.VisitConstant(exp);

            if (member.Expression.NodeType == ExpressionType.Parameter)
            {
                sql.Append(_queryInfo.Config.ApplyNomenclature(member.Member.Name));
                return member;
            }

            return member;
        }

        private Expression VisitField(MemberExpression exp)
        {
            FieldInfo field = exp.Member as FieldInfo;

            args.Add(field.GetValue((exp.Expression as ConstantExpression).Value));
            sql.Append("?");

            return exp;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            bool isProp = node.Arguments[0].NodeType == ExpressionType.Parameter;
            if (node.Method.Name == nameof(string.StartsWith))
            {
                this.Visit(node.Object);
                sql.Append(" LIKE '");
                if (isProp)
                    sql.Append("' + ");
                this.Visit(node.Arguments[0]);
                if (isProp)
                    sql.Append("+ '");
                sql.Append("%'");
            }

            if (node.Method.Name == nameof(string.EndsWith))
            {
                this.Visit(node.Object);
                sql.Append(" LIKE '%");
                if (isProp)
                    sql.Append("' + ");
                this.Visit(node.Arguments[0]);
                if (isProp)
                    sql.Append("+ '");
                sql.Append("'");
            }


            if (node.Method.Name == nameof(string.Contains))
            {
                this.Visit(node.Object);
                sql.Append(" LIKE '%");
                if (isProp)
                    sql.Append("' + ");
                this.Visit(node.Arguments[0]);
                if (isProp)
                    sql.Append("+ '");
                sql.Append("%'");
            }

            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            sql.Append("(");
            Visit(node.Left);

            if (!ExpressionMap.TryGetValue(node.NodeType, out string value))
                throw new NotSupportedException($"The binary operator '{node.NodeType}' is not supported.");

            sql.Append(value);
            Visit(node.Right);
            sql.Append(")");

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            args.Add(node.Value);
            sql.Append("?");

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression == null || node.Expression.NodeType != ExpressionType.Parameter)
                return base.VisitMember(node);

            sql.Append(_queryInfo.Config.ApplyNomenclature(node.Member.Name));
            return node;
        }
    }
}
