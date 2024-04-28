using System;

namespace SharpOrm.Builder
{
    public class TableSchema
    {
        public string SchemaName { get; set; }
        public string Name { get; }
        public bool Temporary { get; set; }

        public readonly BasedTableInfo BasedTable;
        public readonly TableColumnCollection Columns;

        public TableSchema(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            this.Columns = new TableColumnCollection();
            this.Name = name;
        }

        public TableSchema(string name, string basedTableName, bool copyData = false, params Column[] columns)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrEmpty(basedTableName))
                throw new ArgumentNullException(nameof(basedTableName));

            this.BasedTable = new BasedTableInfo(basedTableName, columns, copyData);
            this.Name = name;
        }

        public class BasedTableInfo
        {
            public string Name { get; }
            public Column[] Columns { get; }
            public bool CopyData { get; }

            internal BasedTableInfo(string tableName, Column[] columns, bool copyData)
            {
                Name = tableName;
                Columns = columns;
                CopyData = copyData;
            }
        }
    }
}
