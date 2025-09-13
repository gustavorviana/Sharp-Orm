using SharpOrm.Builder.Grammars.Table.Constraints;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Fb.Grammars
{
    public class FbPrimaryKeyConstraintBuilder : ConstraintBuilder<PrimaryKeyConstraint>
    {
        protected override SqlExpression Build(PrimaryKeyConstraint constraint)
        {
            var sql = new StringBuilder();
            sql.Append("CONSTRAINT ")
                .Append($"{constraint.GetEffectiveName()} ")
                .Append("PRIMARY KEY ")
                .Append($"({string.Join(", ", constraint.Columns)})");

            return new SqlExpression(sql.ToString());
        }
    }
}
