using SharpOrm.Builder.Grammars.Table.Constraints;
using System.Text;

namespace SharpOrm.Builder.Grammars.Sqlite.Builder
{
    /// <summary>
    /// SQLite implementation for building CHECK constraints.
    /// </summary>
    public class SqliteCheckConstraintBuilder : ConstraintBuilder<CheckConstraint>
    {
        protected override SqlExpression Build(CheckConstraint constraint)
        {
            var sql = new StringBuilder()
                .Append("CONSTRAINT ")
                .Append($"\"{constraint.GetEffectiveName()}\" ")
                .Append($"CHECK ({constraint.Expression})");

            return new SqlExpression(sql.ToString());
        }
    }
}
