using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Grammars.Table.Constraints;
using System.Text;

namespace SharpOrm.Builder.Grammars.SqlServer.Builder
{
    public class SqlServerForeignKeyConstraintBuilder : ConstraintBuilder<ForeignKeyConstraint>
    {
        protected override SqlExpression Build(ForeignKeyConstraint constraint)
        {
            var sql = new StringBuilder();

            sql.Append("CONSTRAINT ")
               .Append($"[{constraint.GetEffectiveName()}] ")
               .Append("FOREIGN KEY ")
               .Append($"([{constraint.ForeignKeyColumn}]) ")
               .Append($"REFERENCES [{constraint.ReferencedTable}] ")
               .Append($"([{constraint.ReferencedColumn}])")
               .Append(" ON DELETE ")
               .Append(GetRuleText(constraint.OnDelete))
               .Append(" ON UPDATE ")
               .Append(GetRuleText(constraint.OnUpdate));

            return new SqlExpression(sql.ToString());
        }

        private string GetRuleText(DbRule rule)
        {
            if (rule == DbRule.Cascade) return "CASCADE";
            if (rule == DbRule.SetNull) return "SET NULL";
            if (rule == DbRule.SetDefault) return "SET DEFAULT";
            return "NO ACTION";
        }
    }
}
