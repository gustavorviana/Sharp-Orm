namespace SharpOrm.Builder
{
    public interface IQueryConfig
    {
        /// <summary>
        /// Indicates if value modifications in the table should be made with "WHERE" (this is not valid for insert-and-select).
        /// </summary>
        bool OnlySafeModifications { get; }

        /// <summary>
        /// Gets or sets the wait time before terminating the attempt to execute a command and generating an error.
        /// </summary>
        int CommandTimeout { get; set; }

        /// <summary>
        /// Creates a new grammar object.
        /// </summary>
        /// <param name="query">Query for grammar.</param>
        /// <returns></returns>
        Grammar NewGrammar(Query query);

        /// <summary>
        /// Fix table name, column and alias for SQL.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string ApplyNomenclature(string name);
    }
}
