using SharpOrm.Builder.DataTranslation;
using System;
using System.Globalization;

namespace SharpOrm.Builder
{
    public interface IQueryConfig
    {
        /// <summary>
        /// DateTime kind to be saved in the database.
        /// </summary>
        DateTimeKind DateKind { get; set; }

        /// <summary>
        /// Indicates if value modifications in the table should be made with "WHERE" (this is not valid for insert-and-select).
        /// </summary>
        bool OnlySafeModifications { get; }

        /// <summary>
        /// Gets or sets the wait time before terminating the attempt to execute a command and generating an error.
        /// </summary>
        int CommandTimeout { get; set; }

        /// <summary>
        /// If enabled, allows the query to create an object only with its primary key when there is no depth and allows reading the id of a foreign object.
        /// </summary>
        bool ForeignLoader { get; set; }

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

        TableReaderBase CreateTableReader(string[] tables, int maxDepth);
    }
}
