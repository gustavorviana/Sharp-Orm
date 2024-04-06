using SharpOrm.Builder.DataTranslation;
using System;

namespace SharpOrm.Builder
{
    public abstract class QueryConfig
    {
        public TranslationRegistry Translation { get; set; } = TranslationRegistry.Default;

        /// <summary>
        /// Indicates if value modifications in the table should be made with "WHERE" (this is not valid for insert-and-select).
        /// </summary>
        public bool OnlySafeModifications { get; }

        /// <summary>
        /// Gets or sets the wait time before terminating the attempt to execute a command and generating an error.
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// If enabled, allows the query to create an object only with its primary key when there is no depth and allows reading the id of a foreign object on insert or update.
        /// </summary>
        public bool LoadForeign { get; set; }

        /// <summary>
        /// If true, parameters will be used; if false, strings will be manually escaped.
        /// </summary>
        /// <remarks>
        /// Use this option with caution, as it can cause issues in the execution of your scripts.
        /// </remarks>
        public bool EscapeStrings { get; set; }

        public QueryConfig() : this(true)
        {

        }

        public QueryConfig(bool safeModificationsOnly)
        {
            this.OnlySafeModifications = safeModificationsOnly;
        }

        /// <summary>
        /// Fix table name, column and alias for SQL.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract string ApplyNomenclature(string name);

        /// <summary>
        /// Creates a new grammar object.
        /// </summary>
        /// <param name="query">Query for grammar.</param>
        /// <returns></returns>
        public abstract Grammar NewGrammar(Query query);

        public abstract string EscapeString(string value);
    }
}
