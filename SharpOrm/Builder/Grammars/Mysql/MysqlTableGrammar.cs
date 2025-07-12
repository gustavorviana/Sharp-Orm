using SharpOrm.Builder.Grammars.Mysql.ColumnTypes;
using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Msg;
using System;
using System.Data;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder.Grammars.Mysql
{
    /// <summary>
    /// Provides the implementation for building SQL table-related commands specific to MySQL.
    /// </summary>
    public class MysqlTableGrammar : TableGrammar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MysqlTableGrammar"/> class with the specified configuration and schema.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="schema">The table schema.</param>
        public MysqlTableGrammar(QueryConfig config, TableSchema schema) : base(config, schema)
        {
            //Ref: https://medium.com/dbconvert/mysql-and-sql-servers-data-types-mapping-4cedc95de638
            ColumnTypes.Add(new ColumnType(typeof(int), "INT"));
            ColumnTypes.Add(new ColumnType(typeof(long), "BIGINT"));
            ColumnTypes.Add(new ColumnType(typeof(short), "SMALLINT"));
            ColumnTypes.Add(new ColumnType(typeof(byte), "TINYINT"));
            ColumnTypes.Add(new ColumnType(typeof(float), "FLOAT"));
            ColumnTypes.Add(new ColumnType(typeof(double), "DOUBLE"));
            ColumnTypes.Add(new ColumnType(typeof(decimal), "DECIMAL"));
            ColumnTypes.Add(new ColumnType(typeof(bool), "BIT"));
            ColumnTypes.Add(new ColumnType(typeof(char), "CHAR(1)"));
            ColumnTypes.Add(new ColumnType(typeof(DateTime), "DATETIME"));
            ColumnTypes.Add(new ColumnType(typeof(TimeSpan), "TIME"));
            ColumnTypes.Add(new ColumnType(typeof(byte[]), "BLOB"));
            ColumnTypes.Add(new MysqlStringColumnType());
            ColumnTypes.Add(new GuidColumnType(config.Translation, "CHAR"));
        }

        public override SqlExpression Exists()
        {
            return new SqlExpression(new StringBuilder()
                .Append("CALL sys.table_exists(DATABASE(),")
                .Append('"')
                .Append(Name.Name)
                .Append('"')
                .Append(",@`table_type`);SELECT @`table_type`=")
                .Append('"')
                .Append(Schema.Temporary ? "TEMPORARY" : "BASE TABLE")
                .Append('"')
                .Append(';')
            );
        }

        public override SqlExpression Create()
        {
            if (Schema.BasedQuery != null)
                return CreateBased();

            var query = GetCreateTableQuery()
                .Add('(')
                .AddJoin(",", Schema.Columns.Select(GetColumnDefinition));

            WriteUnique(query);
            WritePk(query);

            return query.Add(')').ToExpression();
        }

        private SqlExpression CreateBased()
        {
            return GetCreateTableQuery()
                .Add(new MysqlGrammar(Schema.BasedQuery).Select())
                .ToExpression();
        }

        private QueryBuilder GetCreateTableQuery()
        {
            QueryBuilder query = GetBuilder();
            query.Add("CREATE ");

            if (Schema.Temporary)
                query.Add("TEMPORARY ");

            query.AddFormat("TABLE {0} ", ApplyNomenclature(Name.Name));

            return query;
        }

        public override SqlExpression Drop()
        {
            var query = GetBuilder();
            query.Add("DROP ");

            if (Schema.Temporary)
                query.Add("TEMPORARY ");

            query.Add("TABLE ").Add(ApplyNomenclature(Name.Name));

            return query.ToExpression();
        }

        private string GetColumnDefinition(DataColumn column)
        {
            if (column.ColumnName.Contains("."))
                throw new InvalidOperationException(Messages.Query.ColumnNotSuportDot);

            string columnName = Config.ApplyNomenclature(column.ColumnName);
            string dataType = GetColumnType(column);
            string autoIncrement = column.AutoIncrement ? " AUTO_INCREMENT" : string.Empty;
            string nullable = column.AllowDBNull ? "DEFAULT NULL" : "NOT NULL";

            return string.Concat(columnName, " ", dataType, " ", nullable, autoIncrement);
        }
    }
}
