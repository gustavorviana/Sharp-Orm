namespace SharpOrm.Builder
{
    public interface IQueryConfig
    {
        string ColumnPrefix { get; set; }
        string ColumnSuffix { get; set; }

        /// <summary>
        /// Indicates if value modifications in the table should be made with "WHERE" (this is not valid for insert-and-select).
        /// </summary>
        bool OnlySafeModifications { get; }

        /// <summary>
        /// Creates a new grammar object.
        /// </summary>
        /// <param name="query">Query for grammar.</param>
        /// <returns></returns>
        Grammar NewGrammar(Query query);
    }
}
