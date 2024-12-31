namespace SharpOrm.Builder.Expressions
{
    internal class ExpressionColumn : Column
    {
        private readonly bool isFk;

        public ExpressionColumn(SqlExpression expression) : base(expression)
        {
        }

        public ExpressionColumn(string name, SqlExpression expression, bool isFk) : base(expression)
        {
            this.Name = name;
            this.isFk = isFk;
        }

        public override SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            QueryBuilder builder = new QueryBuilder(info);

            if (!this.isFk && info is QueryInfo qi && qi.Joins.Count > 0)
                builder.Add(info.Config.ApplyNomenclature(info.TableName.TryGetAlias(info.Config))).Add('.');

            builder.Add(this.expression);

            if (!alias || this.expression.ToString() == this.Alias) return builder.ToExpression();

            if (alias && !string.IsNullOrEmpty(this.Alias))
                builder.Add(" AS ").Add(info.Config.ApplyNomenclature(this.Alias));

            return builder.ToExpression();
        }
    }
}
