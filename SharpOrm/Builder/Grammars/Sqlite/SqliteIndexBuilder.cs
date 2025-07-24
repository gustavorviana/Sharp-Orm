using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Tables;
using System.Text;

namespace SharpOrm.Builder.Grammars.Sqlite
{
    public class SqliteIndexBuilder : IIndexSqlBuilder
    {
        public SqlExpression Build(IndexDefinition indexDefinition)
        {
            var sql = new StringBuilder();

            sql.Append("CREATE ");

            if (indexDefinition.IsUnique)
                sql.Append("UNIQUE ");

            sql.Append("INDEX ")
                .Append($"[{indexDefinition.Name}] ")
                .Append($"ON [{indexDefinition.TableName}] ")
                .Append($"([{string.Join("], [", indexDefinition.Columns)}])");

            if (indexDefinition.Annotations.ContainsKey(IndexAnnotations.Filter))
            {
                var filter = indexDefinition.Annotations[IndexAnnotations.Filter].ToString();
                sql.Append($" WHERE {filter}");
            }

            return new SqlExpression(sql.ToString());
        }
    }
}
