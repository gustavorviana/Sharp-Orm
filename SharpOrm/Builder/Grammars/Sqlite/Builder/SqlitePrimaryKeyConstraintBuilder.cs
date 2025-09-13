using SharpOrm.Builder.Grammars.Table.Constraints;
using System;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder.Grammars.Sqlite.Builder
{
    /// <summary>
    /// SQLite implementation for building PRIMARY KEY constraints.
    /// </summary>
    public class SqlitePrimaryKeyConstraintBuilder : ConstraintBuilder<PrimaryKeyConstraint>
    {
        protected override SqlExpression Build(PrimaryKeyConstraint constraint)
        {
            var sql = new StringBuilder()
                .Append("PRIMARY KEY");

            if (!constraint.AutoIncrement && constraint.Columns.Length > 1)
                return new SqlExpression(sql.Append($" (\"{string.Join("\",\"", constraint.Columns)}\")").ToString());

            if (constraint.Columns.Length > 1)
                throw new InvalidOperationException("SQLite only supports AUTOINCREMENT on single-column primary keys.");

            if (constraint.AutoIncrement)
                sql.Append(" AUTOINCREMENT");

            return new SqlExpression(sql.ToString());
        }
    }
}
