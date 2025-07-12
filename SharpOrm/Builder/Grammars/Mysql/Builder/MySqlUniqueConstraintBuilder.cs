using SharpOrm.Builder.Grammars.Table.Constraints;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Mysql.Builder
{
    public class MySqlUniqueConstraintBuilder : ConstraintBuilder<UniqueConstraint>
    {
        protected override SqlExpression Build(UniqueConstraint constraint)
        {
            var sql = new StringBuilder()
                .Append("CONSTRAINT ")
                .Append($"`{constraint.GetEffectiveName()}` ")
                .Append("UNIQUE ")
                .Append($"(`{string.Join("`, `", constraint.Columns)}`)");

            return new SqlExpression(sql.ToString());
        }
    }
}
