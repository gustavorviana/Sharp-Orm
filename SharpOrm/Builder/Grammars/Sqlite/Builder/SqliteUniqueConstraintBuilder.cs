using SharpOrm.Builder.Grammars.Table.Constraints;
using System.Text;

namespace SharpOrm.Builder.Grammars.Sqlite.Builder
{
    /// <summary>
    /// SQLite implementation for building UNIQUE constraints.
    /// </summary>
    public class SqliteUniqueConstraintBuilder : ConstraintBuilder<UniqueConstraint>
    {
        protected override SqlExpression Build(UniqueConstraint constraint)
        {
            var sql = new StringBuilder()
                .Append("CONSTRAINT ")
                .Append($"\"{constraint.GetEffectiveName()}\" ")
                .Append("UNIQUE ")
                .Append($"(\"{string.Join("\",\"", constraint.Columns)}\")");

            return new SqlExpression(sql.ToString());
        }
    }
}