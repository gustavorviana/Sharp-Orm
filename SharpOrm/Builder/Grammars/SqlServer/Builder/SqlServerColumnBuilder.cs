using SharpOrm.Builder.Grammars.Table;
using System.Data;

namespace SharpOrm.Builder.Grammars.SqlServer.Builder
{
    public class SqlServerColumnBuilder : ColumnSqlBuilder
    {
        public SqlServerColumnBuilder(QueryConfig config, string type, DataColumn column)
            : base(config, type, column)
        {
        }

        public override SqlExpression Build()
        {
            var query = GetBuilder();

            query.AddColumn(Column.ColumnName).Add(' ')
                .Add(GetColumnType());

            if (Column.AutoIncrement)
            {
                var seed = GetIdentitySeed();
                var increment = GetIdentityIncrement();
                query.AddFormat(" IDENTITY({0},{1})", seed, increment);
            }

            if (IsComputed)
            {
                query.Add(" AS ").Add(GetComputedExpression());

                if (GetIsVirtual())
                    query.Add(" PERSISTED");

                return query.ToExpression();
            }

            query.Add(Column.AllowDBNull ? " NULL" : " NOT NULL");

            if (HasDefaultValue)
                query.AddFormat(" DEFAULT {0}", GetDefaultValue());

            var checkConstraint = GetCheckConstraint();
            if (!string.IsNullOrEmpty(checkConstraint))
                query.AddFormat(" CHECK ({0})", checkConstraint);

            var collation = GetCollation();
            if (!string.IsNullOrEmpty(collation))
                query.Add(" COLLATE ").Add(collation);

            return query.ToExpression();
        }
    }
}
