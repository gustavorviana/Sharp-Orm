namespace SharpOrm.Builder.Expressions
{
    internal class NodeMemberColumn : Column, IDeferredSqlExpression
    {
        private readonly DbName _tableName;

        private string tableName = null;

        public NodeMemberColumn(DbName tableName, string columnName)
        {
            _tableName = tableName;
            Name = columnName;
        }

        public override SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            var builder = new QueryBuilder(info);
            builder.Add(ToString());

            if (alias && !string.IsNullOrEmpty(Alias))
                builder.Add(" AS ").Add(Alias);

            return builder.ToExpression();
        }

        public override string ToString()
        {
            return $"{_tableName.TryGetAlias()}.{Name}";
        }
    }
}
