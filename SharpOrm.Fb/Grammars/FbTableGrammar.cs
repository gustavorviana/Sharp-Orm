using SharpOrm.Builder;
using SharpOrm.Builder.Grammars;
using SharpOrm.Builder.Grammars.Firebird;
using SharpOrm.Builder.Grammars.Firebird.ColumnTypes;
using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SharpOrm.Fb.Grammars
{
    public class FbTableGrammar : TableGrammar
    {
        protected override IIndexSqlBuilder IndexBuilder { get; } = new FbIndexBuilder();

        public FbTableGrammar(QueryConfig config, ITableSchema schema) : base(config, schema)
        {
            ColumnTypes.Add(new ColumnType(typeof(int), "INTEGER"));
            ColumnTypes.Add(new ColumnType(typeof(long), "BIGINT"));
            ColumnTypes.Add(new ColumnType(typeof(short), "SMALLINT"));
            ColumnTypes.Add(new ColumnType(typeof(byte), "SMALLINT"));
            ColumnTypes.Add(new ColumnType(typeof(float), "FLOAT"));
            ColumnTypes.Add(new ColumnType(typeof(double), "DOUBLE PRECISION"));
            ColumnTypes.Add(new ColumnType(typeof(decimal), "DECIMAL"));
            ColumnTypes.Add(new ColumnType(typeof(bool), "SMALLINT"));
            ColumnTypes.Add(new ColumnType(typeof(char), "CHAR(1)"));
            ColumnTypes.Add(new ColumnType(typeof(DateTime), "TIMESTAMP"));
            ColumnTypes.Add(new ColumnType(typeof(TimeSpan), "TIME"));
            ColumnTypes.Add(new ColumnType(typeof(byte[]), "BLOB"));
            ColumnTypes.Add(new FbStringColumnType());
            ColumnTypes.Add(new GuidColumnType(config.Translation, "CHAR"));

            ConstraintBuilders.Add(new FbPrimaryKeyConstraintBuilder());
            ConstraintBuilders.Add(new FbForeignKeyConstraintBuilder());
            ConstraintBuilders.Add(new FbUniqueConstraintBuilder());
            ConstraintBuilders.Add(new FbCheckConstraintBuilder());
        }

        public override SqlExpression Exists()
        {
            return new SqlExpression(new StringBuilder()
                .Append("SELECT COUNT(*) FROM RDB$RELATIONS WHERE RDB$RELATION_NAME = '")
                .Append(Name.Name.ToUpper())
                .Append("' AND RDB$RELATION_TYPE = ")
                .Append(Schema.Temporary ? "4" : "0")
            );
        }

        public override SqlExpression Create()
        {
            if (BasedQuery != null)
                return CreateBased();
            var query = GetCreateTableQuery()
                .Add('(')
                .AddJoin(",", Schema.Columns.Select(GetColumnDefinition));
            WriteConstraints(query);
            return query.Add(')').ToExpression();
        }

        private SqlExpression CreateBased()
        {
            return GetCreateTableQuery()
                .Add("AS ")
                .Add(new FbGrammar(BasedQuery).Select())
                .ToExpression();
        }

        private QueryBuilder GetCreateTableQuery()
        {
            QueryBuilder query = GetBuilder();
            query.Add("CREATE ");
            if (Schema.Temporary)
                query.Add("GLOBAL TEMPORARY ");
            query.AddFormat("TABLE {0} ", ApplyNomenclature(Name.Name));
            return query;
        }

        public override SqlExpression Drop()
        {
            var query = GetBuilder();
            query.Add("DROP TABLE ").Add(ApplyNomenclature(Name.Name));
            return query.ToExpression();
        }

        private string GetColumnDefinition(DataColumn column)
        {
            if (column.ColumnName.Contains("."))
                throw new InvalidOperationException(Messages.Query.ColumnNotSuportDot);

            string columnName = Config.ApplyNomenclature(column.ColumnName);
            string dataType = GetColumnType(column);
            string nullable = column.AllowDBNull ? "" : "NOT NULL";
            string defaultValue = GetDefaultValue(column);

            return string.Concat(columnName, " ", dataType, " ", nullable, defaultValue).Trim();
        }

        private string GetDefaultValue(DataColumn column)
        {
            if (column.DefaultValue != null && column.DefaultValue != DBNull.Value)
            {
                return $" DEFAULT {FormatDefaultValue(column.DefaultValue)}";
            }
            return string.Empty;
        }

        private string FormatDefaultValue(object value)
        {
            if (value is string)
                return $"'{value.ToString().Replace("'", "''")}'";
            if (value is DateTime)
                return "'NOW'";
            if (value is bool)
                return (bool)value ? "1" : "0";
            return value.ToString();
        }
    }
}
