using System.Text;

namespace SharpOrm.Builder
{
    public class SqlServerQueryConfig : QueryConfig
    {
        private const char StrDelimitor = '\'';
        public bool UseOldPagination { get; set; }

        public SqlServerQueryConfig()
        {
        }

        public SqlServerQueryConfig(bool onlySafeModifications) : base(onlySafeModifications)
        {
        }

        public override string ApplyNomenclature(string name)
        {
            return name.SanitizeSqlName('[', ']');
        }

        public override Grammar NewGrammar(Query query)
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

        public override string EscapeString(string value)
        {
            StringBuilder builder = new StringBuilder(value.Length).Append(StrDelimitor);

            foreach (var c in value)
            {
                if (c == StrDelimitor) builder.Append(StrDelimitor);
                builder.Append(c);
            }

            return builder.Append(StrDelimitor).ToString();
        }
    }
}
