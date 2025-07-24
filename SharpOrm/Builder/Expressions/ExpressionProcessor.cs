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
    internal class ExpressionProcessor<T>
    {
        private readonly IReadonlyQueryInfo _info;
        private readonly ExpressionConfig _config;
        private readonly SqlExpressionVisitor _visitor;

        internal ExpressionProcessor(IFkNodeRoot root, ExpressionConfig config) : this(root.ForeignKeyRegister.RootInfo, root.ForeignKeyRegister?.RootInfo?.Config?.Translation, config, root.ForeignKeyRegister)
        {

        }

        internal ExpressionProcessor(IReadonlyQueryInfo info, TranslationRegistry registry, ExpressionConfig config, IForeignKeyNode parent)
        {
            _info = info;
            _config = config;
            _visitor = new SqlExpressionVisitor(typeof(T), registry, info, config, parent);
        }

        internal IEnumerable<string> ParseColumnNames(Expression<ColumnExpression<T>> expression)
        {
            if (expression is null)
                return DotnetUtils.EmptyArray<string>();

            return ParseExpression(expression).Select(x => x.Name);
        }

        public IEnumerable<ExpressionColumn> ParseColumns(Expression<ColumnExpression<T>> expression)
        {
            return ParseExpression(expression).Select(BuildColumn);
        }

        public string GetTableName<R>(Expression<ColumnExpression<T, R>> expression, out MemberInfo memberInfo)
        {
            var member = InternalParseExpression(expression).First();
            if (member.IsNativeType)
                throw new NotSupportedException(Messages.Expressions.NativeTypeInTableName);

            memberInfo = member.Member;
            return _info.Config.Translation.GetTableName(ReflectionUtils.GetMemberType(member.Member));
        }

        private SqlMember ProcessMemberInfo(SqlMember info)
        {
            if (info.Childs.Length > 1 && info.Childs.Last().Name == nameof(object.ToString))
            {
                var toCheck = info.Childs[info.Childs.Length - 2];
                bool isDateOrTime = TranslationUtils.IsDateOrTime(toCheck.DeclaringType);
                bool isTimeOfDay = toCheck.Name == nameof(DateTime.TimeOfDay);

                info.Childs = isDateOrTime && isTimeOfDay
                    ? info.Childs.Where((x, index) => index != info.Childs.Length - 2).ToArray()
                    : info.Childs;
            }

            return info;
        }

        public ExpressionColumn ParseColumn<R>(Expression<ColumnExpression<T, R>> expression)
        {
            return BuildColumn(ParseExpressionField<R>(expression));
        }

        private ExpressionColumn BuildColumn(SqlMember member)
        {
            var sqlExpression = _info.Config.Methods.ApplyMember(_info, ProcessMemberInfo(member), _visitor._parent);
            return new ExpressionColumn(member.Member, sqlExpression)
            {
                Alias = member.Alias ?? (member.Childs.Length > 0 ? member.Name : null)
            };
        }

        public SqlMember ParseExpressionField<R>(Expression<ColumnExpression<T, R>> expression)
        {
            if (!(expression.Body is NewExpression newExpression))
                return _visitor.Visit(expression.Body);

            if (!_config.HasFlag(ExpressionConfig.New))
                throw new NotSupportedException(Messages.Expressions.NewExpressionDisabled);

            return _visitor.Visit(
               newExpression.Arguments.First(),
               newExpression.Members.First().Name
           );
        }

        public IEnumerable<SqlMember> ParseExpression(Expression<ColumnExpression<T>> expression)
        {
            return InternalParseExpression(expression);
        }

        private IEnumerable<SqlMember> InternalParseExpression(LambdaExpression expression)
        {
            if (expression.Body is NewExpression newExpression)
            {
                foreach (var item in ParseExpression(newExpression))
                    yield return item;
            }
            else
            {
                yield return _visitor.Visit(expression.Body);
            }
        }

        private IEnumerable<SqlMember> ParseExpression(NewExpression newExpression)
        {
            if (!_config.HasFlag(ExpressionConfig.New))
                throw new NotSupportedException(Messages.Expressions.NewExpressionDisabled);

            for (int i = 0; i < newExpression.Members.Count; i++)
                yield return _visitor.Visit(
                    newExpression.Arguments[i],
                    newExpression.Members[i].Name
                );
        }
    }
}