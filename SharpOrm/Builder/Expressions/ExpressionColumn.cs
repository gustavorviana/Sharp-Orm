using System.Reflection;

namespace SharpOrm.Builder.Expressions
{
    internal class ExpressionColumn : Column
    {
        public readonly MemberInfo memberInfo;

        public ExpressionColumn(MemberInfo memberInfo, SqlExpression expression) : base(expression)
        {
            this.Name = ColumnInfo.GetName(memberInfo);
            this.memberInfo = memberInfo;
        }

        public override SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            QueryBuilder builder = new QueryBuilder(info);

            builder.Add(this.expression);

            if (alias && !string.IsNullOrEmpty(this.Alias))
                builder.Add(" AS ").Add(info.Config.ApplyNomenclature(this.Alias));

            return builder.ToExpression();
        }
    }
}
