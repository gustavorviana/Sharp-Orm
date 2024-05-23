using System.Data;
using System.Linq;

namespace SharpOrm.Builder
{
    public abstract class TableGrammar
    {
        protected readonly IReadonlyQueryInfo queryInfo;
        public TableSchema Schema { get; }
        public QueryConfig Config => this.queryInfo.Config;

        /// <summary>
        /// Database name in the database's standard format.
        /// </summary>
        public virtual DbName Name { get; }
        protected QueryInfo BasedTable => this.Schema.BasedQuery.Info;

        public TableGrammar(QueryConfig config, TableSchema schema)
        {
            this.Schema = schema;
            this.Name = this.LoadName();
            this.queryInfo = new ReadonlyQueryInfo(config, this.Name);
        }

        protected virtual DbName LoadName()
        {
            return new DbName(Schema.Name);
        }

        public abstract SqlExpression Create();

        protected void WriteColumns(QueryBuilder query, Column[] columns)
        {
            if (columns.Length == 0)
            {
                query.Add("*");
                return;
            }

            query.AddExpression(columns[0]);

            for (int i = 1; i < columns.Length; i++)
                query.Add(",").AddExpression(columns[i]);
        }

        public abstract SqlExpression Drop();

        public abstract SqlExpression Exists();

        protected ColumnTypeMap GetCustomColumnTypeMap(DataColumn column)
        {
            return this.queryInfo
                .Config
                .CustomColumnTypes?
                .FirstOrDefault(x => x.CanWork(column.DataType));
        }

        protected virtual void WritePk(QueryBuilder query)
        {
            var pks = this.GetPrimaryKeys();
            if (pks.Length != 0)
                query.AddFormat(",CONSTRAINT {0} PRIMARY KEY (", this.Config.ApplyNomenclature(string.Concat("PK_", this.Name))).AddJoin(",", pks.Select(x => this.Config.ApplyNomenclature(x.ColumnName))).Add(')');
        }

        protected DataColumn[] GetPrimaryKeys()
        {
            return this.Schema.Columns.PrimaryKeys;
        }

        protected void WriteUnique(QueryBuilder query)
        {
            var uniques = this.GetUniqueKeys();
            if (uniques.Length != 0)
                query.AddFormat(",CONSTRAINT {0} UNIQUE (", this.Config.ApplyNomenclature(string.Concat("UC_", this.Name))).AddJoin(",", uniques.Select(x => this.Config.ApplyNomenclature(x.ColumnName))).Add(')');
        }

        protected DataColumn[] GetUniqueKeys()
        {
            return this.Schema.Columns.Where(x => x.Unique).ToArray();
        }

        protected QueryBuilder GetBuilder()
        {
            return new QueryBuilder(queryInfo);
        }

        protected string ApplyNomenclature(string name)
        {
            return this.queryInfo.Config.ApplyNomenclature(name);
        }


        /// <summary>
        /// Ref: https://learn.microsoft.com/pt-br/dotnet/api/system.guid.tostring?view=net-8.0
        /// </summary>
        /// <returns></returns>
        protected int GetGuidSize()
        {
            switch (this.Config.Translation.GuidFormat)
            {
                case "N": return 32;
                case "D": return 36;
                case "B":
                case "P": return 38;
                default: return 68;
            }
        }
    }
}
