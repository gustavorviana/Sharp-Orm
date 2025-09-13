using SharpOrm.Builder.Grammars.Table.Constraints;
using System.Text;

namespace SharpOrm.Fb.Grammars
{
    /// <summary>
    /// Firebird implementation for building CHECK constraints.
    /// CHECK constraints are fully supported in all Firebird versions.
    /// </summary>
    public class FbCheckConstraintBuilder : ConstraintBuilder<CheckConstraint>
    {
        protected override SqlExpression Build(CheckConstraint constraint)
        {
            var sql = new StringBuilder()
                .Append("CONSTRAINT ")
                .Append($"{constraint.GetEffectiveName()} ")
                .Append($"CHECK ({constraint.Expression})");

            return new SqlExpression(sql.ToString());
        }
    }
}