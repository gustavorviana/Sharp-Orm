using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Tables;
using SharpOrm.Fb;
using System.Text;

namespace SharpOrm.Builder.Grammars.Firebird
{
    public class FbIndexBuilder : IIndexSqlBuilder
    {
        public SqlExpression Build(IndexDefinition indexDefinition)
        {
            var sql = new StringBuilder();
            sql.Append("CREATE ");

            if (indexDefinition.IsUnique)
                sql.Append("UNIQUE ");

            sql.Append("INDEX ")
                .Append($"{indexDefinition.GetEffectiveName()} ")
                .Append($"ON {indexDefinition.TableName} ")
                .Append($"({string.Join(", ", indexDefinition.Columns)})");

            // Firebird suporta ordem de classificação por coluna
            if (indexDefinition.Annotations.ContainsKey(FbIndexNotations.SortOrder))
            {
                var sortOrder = indexDefinition.Annotations[FbIndexNotations.SortOrder].ToString();
                sql.Append($" {sortOrder}");
            }

            // Firebird suporta índices computados
            if (indexDefinition.Annotations.ContainsKey(FbIndexNotations.ComputedBy))
            {
                var computedExpression = indexDefinition.Annotations[FbIndexNotations.ComputedBy].ToString();
                sql.Clear();
                sql.Append("CREATE ");
                if (indexDefinition.IsUnique)
                    sql.Append("UNIQUE ");
                sql.Append("INDEX ")
                    .Append($"{indexDefinition.GetEffectiveName()} ")
                    .Append($"ON {indexDefinition.TableName} ")
                    .Append($"COMPUTED BY ({computedExpression})");
            }

            return new SqlExpression(sql.ToString());
        }
    }
}