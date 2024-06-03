namespace SharpOrm.Builder
{
    public class SqliteQueryConfig : QueryConfig
    {
        public override bool CanUpdateJoin { get; } = false;

        /// <summary>
        /// Create an instance that allows only safe modifications.
        /// </summary>
        public SqliteQueryConfig()
        {

        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="safeModificationsOnly">Signal whether only safe modifications should be made.</param>
        /// <remarks>Safe modifications are updates and deletes with a WHERE clause.</remarks>
        public SqliteQueryConfig(bool safeModificationsOnly) : base(safeModificationsOnly)
        {
        }

        public override string ApplyNomenclature(string name)
        {
            return name.SanitizeSqlName('"', '"');
        }

        public override Grammar NewGrammar(Query query)
        {
            return new SqliteGrammar(query);
        }

        public override TableGrammar NewTableGrammar(TableSchema schema)
        {
            return new SqliteTableGrammar(this, schema);
        }

        public override string EscapeString(string value) => MysqlQueryConfig.Escape(value);

        public override QueryConfig Clone()
        {
            var clone = new SqliteQueryConfig(this.OnlySafeModifications);
            this.CopyTo(clone);
            return clone;
        }
    }
}
