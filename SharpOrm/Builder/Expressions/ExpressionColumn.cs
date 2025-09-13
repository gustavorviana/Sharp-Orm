using System.Reflection;

namespace SharpOrm.Builder.Expressions
{
    internal class ExpressionColumn : Column
    {
        public readonly MemberInfo _memberInfo;

        public ExpressionColumn(MemberInfo memberInfo, SqlExpression expression) : base(expression)
        {
            Name = ColumnInfo.GetName(memberInfo);
            _memberInfo = memberInfo;
        }

        public override SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            QueryBuilder builder = new QueryBuilder(info);

            builder.Add(expression);

            if (alias && !string.IsNullOrEmpty(Alias))
                builder.Add(" AS ").Add(info.Config.ApplyNomenclature(this.Alias));

            return builder.ToExpression();
        }
    }
}
