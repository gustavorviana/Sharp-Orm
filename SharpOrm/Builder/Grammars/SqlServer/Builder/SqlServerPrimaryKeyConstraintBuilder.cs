using SharpOrm.Builder.Grammars.Table.Constraints;
using System;
using System.Text;

namespace SharpOrm.Builder.Grammars.SqlServer.Builder
{
    public class SqlServerPrimaryKeyConstraintBuilder : ConstraintBuilder<PrimaryKeyConstraint>
    {
        protected override SqlExpression Build(PrimaryKeyConstraint constraint)
        {
            var sql = new StringBuilder();

            sql.Append("CONSTRAINT ")
                .Append($"[{constraint.GetEffectiveName()}] ")
                .Append("PRIMARY KEY ");

            if (constraint.IsClustered.HasValue)
                sql.Append(constraint.IsClustered.Value ? "CLUSTERED " : "NONCLUSTERED ");

            sql.Append($"([{string.Join("],[", constraint.Columns)}])");

            return new SqlExpression(sql.ToString());
        }
    }
}
