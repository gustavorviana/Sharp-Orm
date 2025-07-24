using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Grammars.Table.Constraints;
using System.Text;

namespace SharpOrm.Builder.Grammars.Sqlite.Builder
{
    /// <summary>
    /// SQLite implementation for building FOREIGN KEY constraints.
    /// </summary>
    public class SqliteForeignKeyConstraintBuilder : ConstraintBuilder<ForeignKeyConstraint>
    {
        protected override SqlExpression Build(ForeignKeyConstraint constraint)
        {
            var sql = new StringBuilder()
                .Append("CONSTRAINT ")
                .Append($"\"{constraint.GetEffectiveName()}\" ")
                .Append("FOREIGN KEY ")
                .Append($"(\"{constraint.ForeignKeyColumn}\") ")
                .Append($"REFERENCES \"{constraint.ReferencedTable}\" ")
                .Append($"(\"{constraint.ReferencedColumn}\")");

            if (constraint.OnDelete != DbRule.None)
                sql.Append($" ON DELETE {GetRuleText(constraint.OnDelete)}");

            if (constraint.OnUpdate != DbRule.None)
                sql.Append($" ON UPDATE {GetRuleText(constraint.OnUpdate)}");

            return new SqlExpression(sql.ToString());
        }
    }
}
