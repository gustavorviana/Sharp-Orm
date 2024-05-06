using System;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents the schema for creating a table in the database.
    /// </summary>
    public class TableSchema
    {
        /// <summary>
        /// Schema name in the database.
        /// </summary>
        public string SchemaName { get; set; }
        /// <summary>
        /// Table name.
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Indicate whether the table should be temporary.
        /// </summary>
        public bool Temporary { get; set; }

        /// <summary>
        /// Table whose structure should be copied to the new table.
        /// </summary>
        public readonly Query BasedQuery;
        /// <summary>
        /// Columns to be inserted into the new table.
        /// </summary>
        public readonly TableColumnCollection Columns;

        public TableSchema(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Columns = new TableColumnCollection();
            Name = name;
        }

        public TableSchema(string name, TableColumnCollection columns)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Columns = columns;
            Name = name;
        }

        public TableSchema(string name, Query basedTableQuery)
        {
            this.Name = name;
            this.BasedQuery = basedTableQuery;
        }

        public TableSchema Clone()
        {
            if (this.BasedQuery != null)
                return new TableSchema(this.Name, this.BasedQuery) { Temporary = this.Temporary, SchemaName = this.SchemaName };

            return new TableSchema(this.Name, this.Columns) { Temporary = this.Temporary, SchemaName = this.SchemaName };
        }
    }
}
