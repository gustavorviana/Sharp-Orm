using System;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents the schema for creating a table in the database.
    /// </summary>
    public class TableSchema
    {
        /// <summary>
        /// Gets the table name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether the table should be temporary.
        /// </summary>
        public bool Temporary { get; set; }

        /// <summary>
        /// Gets the query whose structure should be copied to the new table.
        /// </summary>
        public readonly Query BasedQuery;

        /// <summary>
        /// Gets the collection of columns to be inserted into the new table.
        /// </summary>
        public readonly TableColumnCollection Columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableSchema"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <exception cref="ArgumentNullException">Thrown when the name is null or empty.</exception>
        public TableSchema(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Columns = new TableColumnCollection();
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableSchema"/> class with the specified name and columns.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <param name="columns">The collection of columns.</param>
        /// <exception cref="ArgumentNullException">Thrown when the name is null or empty.</exception>
        public TableSchema(string name, TableColumnCollection columns)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Columns = columns;
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableSchema"/> class with the specified name and based query.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        /// <param name="basedTableQuery">The query whose structure should be copied to the new table.</param>
        public TableSchema(string name, Query basedTableQuery)
        {
            this.Name = name;
            this.BasedQuery = basedTableQuery;
        }

        /// <summary>
        /// Creates a clone of the current table schema.
        /// </summary>
        /// <returns>A new instance of <see cref="TableSchema"/> that is a copy of the current instance.</returns>
        public TableSchema Clone()
        {
            if (this.BasedQuery != null)
                return new TableSchema(this.Name, this.BasedQuery) { Temporary = this.Temporary };

            return new TableSchema(this.Name, this.Columns) { Temporary = this.Temporary };
        }
    }
}
