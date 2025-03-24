using SharpOrm.Builder.Grammars;
using SharpOrm.Builder.Grammars.SqlServer;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides extension methods for enabling and checking the NOLOCK option in SQL Server grammar options.
    /// </summary>
    public static class SqlServerExt
    {
        /// <summary>
        /// Enables the NOLOCK option in the SQL Server grammar options.
        /// </summary>
        /// <param name="options">The grammar options.</param>
        public static void EnableNoLock(this IGrammarOptions options)
        {
            options.GrammarOptions = new SqlServerGrammarOptions
            {
                NoLock = true
            };
        }

        /// <summary>
        /// Checks if the NOLOCK option is enabled in the SQL Server grammar options.
        /// </summary>
        /// <param name="options">The grammar options.</param>
        /// <returns>True if the NOLOCK option is enabled; otherwise, false.</returns>
        public static bool IsNoLock(this IGrammarOptions options)
        {
            return options.GrammarOptions is SqlServerGrammarOptions opt && opt.NoLock;
        }
    }
}
