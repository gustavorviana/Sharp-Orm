using SharpOrm.Builder.Grammars.Table.Constraints;
using System.Text;

namespace SharpOrm.Builder.Grammars.SqlServer.Builder
{
    public class SqlServerCheckConstraintBuilder : ConstraintBuilder<CheckConstraint>
    {
        protected override SqlExpression Build(CheckConstraint constraint)
        {
            var sql = new StringBuilder();

            sql.Append("CONSTRAINT ")
                .Append($"[{constraint.GetEffectiveName()}] ")
                .Append($"CHECK ({constraint.Expression})");

            return new SqlExpression(sql.ToString());
        }
    }
}
