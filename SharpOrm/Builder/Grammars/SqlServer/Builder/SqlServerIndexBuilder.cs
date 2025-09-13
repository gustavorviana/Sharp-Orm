using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Tables;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.SqlServer.Builder
{
    public class SqlServerIndexBuilder : IIndexSqlBuilder
    {
        public SqlExpression Build(IndexDefinition indexDefinition)
        {
            var sql = new StringBuilder();

            sql.Append("CREATE ");

            if (indexDefinition.IsUnique)
                sql.Append("UNIQUE ");

            sql.Append(indexDefinition.IsClustered ? "CLUSTERED " : "NONCLUSTERED ")
                .Append("INDEX ")
                .Append($"[{indexDefinition.GetEffectiveName()}] ")
                .Append($"ON [{indexDefinition.TableName}] ")
                .Append($"([{string.Join("], [", indexDefinition.Columns)}])");

            if (indexDefinition.Annotations.ContainsKey(IndexAnnotations.IncludedColumns))
            {
                var includedColumns = (string[])indexDefinition.Annotations[IndexAnnotations.IncludedColumns];
                if (includedColumns.Length > 0)
                {
                    sql.Append($" INCLUDE ([{string.Join("], [", includedColumns)}])");
                }
            }

            if (indexDefinition.Annotations.ContainsKey(IndexAnnotations.Filter))
            {
                var filter = indexDefinition.Annotations[IndexAnnotations.Filter].ToString();
                sql.Append($" WHERE {filter}");
            }

            var withOptions = BuildWithOptions(indexDefinition);
            if (!string.IsNullOrEmpty(withOptions))
            {
                sql.Append($" WITH ({withOptions})");
            }

            if (indexDefinition.Annotations.ContainsKey(IndexAnnotations.Filegroup))
            {
                var filegroup = indexDefinition.Annotations[IndexAnnotations.Filegroup].ToString();
                sql.Append($" ON [{filegroup}]");
            }

            return new SqlExpression(sql.ToString());
        }

        private string BuildWithOptions(IndexDefinition indexDefinition)
        {
            var options = new List<string>();

            if (indexDefinition.Annotations.ContainsKey(IndexAnnotations.FillFactor))
            {
                var fillFactor = indexDefinition.Annotations[IndexAnnotations.FillFactor];
                options.Add($"FILLFACTOR = {fillFactor}");
            }

            if (indexDefinition.Annotations.ContainsKey(IndexAnnotations.Online))
            {
                var online = (bool)indexDefinition.Annotations[IndexAnnotations.Online];
                options.Add($"ONLINE = {(online ? "ON" : "OFF")}");
            }

            if (indexDefinition.Annotations.ContainsKey(IndexAnnotations.IgnoreDuplicateKeys))
            {
                var ignoreDuplicateKeys = (bool)indexDefinition.Annotations[IndexAnnotations.IgnoreDuplicateKeys];
                options.Add($"IGNORE_DUP_KEY = {(ignoreDuplicateKeys ? "ON" : "OFF")}");
            }

            return string.Join(", ", options);
        }
    }
}
