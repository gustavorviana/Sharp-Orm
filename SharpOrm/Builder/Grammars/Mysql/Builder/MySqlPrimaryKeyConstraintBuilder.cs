using SharpOrm.Builder.Grammars.Table.Constraints;
using System.Text;

namespace SharpOrm.Builder.Grammars.Mysql.Builder
{
    public class MySqlPrimaryKeyConstraintBuilder : ConstraintBuilder<PrimaryKeyConstraint>
    {
        protected override SqlExpression Build(PrimaryKeyConstraint constraint)
        {
            var sql = new StringBuilder();

            sql.Append("CONSTRAINT ")
                .Append($"`{constraint.GetEffectiveName()}` ")
                .Append("PRIMARY KEY ")
                .Append($"(`{string.Join("`,`", constraint.Columns)}`)");

            return new SqlExpression(sql.ToString());
        }
    }
}
