using SharpOrm.Builder.Grammars.Table.Constraints;
using System.Text;

namespace SharpOrm.Builder.Grammars.SqlServer.Builder
{
    public class SqlServerUniqueConstraintBuilder : ConstraintBuilder<UniqueConstraint>
    {
        protected override SqlExpression Build(UniqueConstraint constraint)
        {
            var sql = new StringBuilder();

            sql.Append("CONSTRAINT ")
                .Append($"[{constraint.GetEffectiveName()}] ")
                .Append("UNIQUE ")
                .Append($"([{string.Join("], [", constraint.Columns)}])");

            return new SqlExpression(sql.ToString());
        }
    }
}
