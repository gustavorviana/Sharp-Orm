namespace SharpOrm.Builder
{
    public interface IQueryConfig
    {
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

        /// <summary>
        /// Apply column prefix and suffix.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string ApplyNomenclature(string name);
    }
}
