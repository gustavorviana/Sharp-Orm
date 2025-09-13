using SharpOrm.Builder.Grammars.Table.Constraints;
using System.Text;

namespace SharpOrm.Builder.Grammars.Mysql.Builder
{
    /// <summary>
    /// MySQL implementation for building CHECK constraints.
    /// Note: CHECK constraints are available in MySQL 8.0.16+
    /// </summary>
    public class MySqlCheckConstraintBuilder : ConstraintBuilder<CheckConstraint>
    {
        protected override SqlExpression Build(CheckConstraint constraint)
        {
            var sql = new StringBuilder()
                .Append("CONSTRAINT ")
                .Append($"`{constraint.GetEffectiveName()}` ")
                .Append($"CHECK ({constraint.Expression})");

            return new SqlExpression(sql.ToString());
        }
    }
}
