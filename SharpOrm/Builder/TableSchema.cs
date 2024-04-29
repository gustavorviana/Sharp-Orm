using System;

namespace SharpOrm.Builder
{
    public class TableSchema
    {
        public string SchemaName { get; set; }
        public string Name { get; }
        public bool Temporary { get; set; } = true;

        public readonly BaseTable Based;
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

        public class BaseTable
        {
            public string Name { get; }
            public Column[] Columns { get; }
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
