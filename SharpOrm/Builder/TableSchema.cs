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
        public string Name { get; }
        /// <summary>
        /// Indicate whether the table should be temporary.
        /// </summary>
        public bool Temporary { get; set; } = true;

        /// <summary>
        /// Table whose structure should be copied to the new table.
        /// </summary>
        public readonly BaseTable Based;
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

        public TableSchema(string name, string basedTableName, bool copyData = false, params Column[] columns)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Based = new BaseTable(basedTableName, copyData, columns);
            Name = name;
        }

        private TableSchema(string name, BaseTable basedTable)
        {
            this.Name = name;
            this.Based = basedTable;
        }

        public TableSchema Clone()
        {
            if (this.Based != null)
                return new TableSchema(this.Name, this.Based);

            return new TableSchema(this.Name, this.Columns);
        }

        /// <summary>
        /// Represents a table that will serve as the basis for creating another.
        /// </summary>
        public class BaseTable
        {
            /// <summary>
            /// Name of the table to be copied.
            /// </summary>
            public string Name { get; }
            /// <summary>
            /// Columns of the table to be copied.
            /// </summary>
            public Column[] Columns { get; }
            /// <summary>
            /// Indicate whether the data from the table should be copied to the new table.
            /// </summary>
            public bool CopyData { get; }

            internal BaseTable(string tableName, bool copyData = true, params Column[] columns)
            {
                if (string.IsNullOrEmpty(tableName))
                    throw new ArgumentNullException(nameof(tableName));

                Name = tableName;
                Columns = columns;
                CopyData = copyData;
            }
        }
    }
}
