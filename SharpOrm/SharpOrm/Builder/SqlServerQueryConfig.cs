namespace SharpOrm.Builder
{
    public class SqlServerQueryConfig : IQueryConfig
    {
        public bool OnlySafeModifications { get; }

        public SqlServerQueryConfig(bool onlySafeModifications)
        {
            this.OnlySafeModifications = onlySafeModifications;
        }

        public string ApplyNomenclature(string name)
        {
            return $"[{string.Join("].[", name.AlphaNumericOnly('_', '.').Split('.'))}]";
        }

        public Grammar NewGrammar(Query query)
        {
            return new SqlServerGrammar(query);
        }

        /// <summary>
        /// creates a connection line for the local connection: Data Source=localhost\SQLEXPRESS;Initial Catalog={catalog};Integrated Security=True;
        /// </summary>
        /// <param name="initialCatalog"></param>
        /// <returns></returns>
        public static string GetLocalConnectionString(string initialCatalog)
        {
            return $@"Data Source=localhost\SQLEXPRESS;Initial Catalog={initialCatalog};Integrated Security=True";
        }
    }
}
