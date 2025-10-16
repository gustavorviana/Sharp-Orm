namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides methods to inspect and retrieve column information from database tables.
    /// </summary>
    public interface IColumnInspector
    {
        /// <summary>
        /// Creates a SQL expression for retrieving column metadata from a table.
        /// </summary>
        /// <returns>The SQL expression for retrieving column information.</returns>
        SqlExpression GetColumnsQuery();

        /// <summary>
        /// Maps rows from the columns query result to an array of <see cref="DbColumnInfo"/> objects.
        /// </summary>
        /// <param name="rows">The row collection from the query result.</param>
        /// <returns>An array of <see cref="DbColumnInfo"/> instances with column metadata.</returns>
        DbColumnInfo[] MapToColumnInfo(RowDataReader rows);
    }
}
