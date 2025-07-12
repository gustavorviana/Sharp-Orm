using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Tables;
using System.Text;

namespace SharpOrm.Builder.Grammars.Mysql
{
    public class MySqlIndexBuilder : IIndexSqlBuilder
    {
        public SqlExpression Build(IndexDefinition indexDefinition)
        {
            var sql = new StringBuilder();

            sql.Append("CREATE ");

            if (indexDefinition.IsUnique)
                sql.Append("UNIQUE ");

            sql.Append("INDEX ")
                .Append($"`{indexDefinition.GetEffectiveName()}` ")
                .Append($"ON `{indexDefinition.TableName}` ")
                .Append($"(`{string.Join("`, `", indexDefinition.Columns)}`)");

            if (indexDefinition.Annotations.ContainsKey(IndexAnnotations.IndexType))
            {
                var indexType = indexDefinition.Annotations[IndexAnnotations.IndexType].ToString();
                sql.Append($" USING {indexType}");
            }

            if (indexDefinition.Annotations.ContainsKey(IndexAnnotations.KeyBlockSize))
            {
                var keyBlockSize = indexDefinition.Annotations[IndexAnnotations.KeyBlockSize];
                sql.Append($" KEY_BLOCK_SIZE = {keyBlockSize}");
            }

            if (indexDefinition.Annotations.ContainsKey(IndexAnnotations.Comment))
            {
                var comment = indexDefinition.Annotations[IndexAnnotations.Comment].ToString();
                sql.Append($" COMMENT '{comment.Replace("'", "''")}'");
            }

            return new SqlExpression(sql.ToString());
        }
    }
}
