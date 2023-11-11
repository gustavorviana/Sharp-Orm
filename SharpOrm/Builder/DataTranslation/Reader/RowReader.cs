using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    internal class RowReader
    {
        private readonly Dictionary<int, ColumnInfo> colsMap = new Dictionary<int, ColumnInfo>();
        private readonly Dictionary<int, ColumnInfo> fkMap = new Dictionary<int, ColumnInfo>();

        private readonly TableReader reader;
        private readonly TableInfo table;
        private bool hasFirstRead = false;

        public RowReader(TableReader reader, TableInfo table)
        {
            this.reader = reader;
            this.table = table;
        }

        public object GetRow(DbDataReader reader)
        {
            var owner = this.table.CreateInstance();
            if (hasFirstRead)
            {
                foreach (var kv in colsMap)
                    kv.Value.Set(owner, this.reader.ReadDbObject(reader[kv.Key]));

                this.LoadFkObjs(owner, reader);
                return owner;
            }

            var columns = new List<ColumnInfo>(table.Columns.Where(c => !c.IsForeignKey));
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i).ToLower();
                if (columns.FirstOrDefault(c => name == c.Name.ToLower()) is ColumnInfo ci)
                {
                    ci.Set(owner, this.reader.ReadDbObject(reader[i]));
                    columns.Remove(ci);
                    colsMap[i] = ci;
                }
            }

            this.LoadFkObjs(owner, reader);

            this.hasFirstRead = true;
            return owner;
        }

        private void LoadFkObjs(object owner, DbDataReader reader)
        {
            if (!this.table.HasFk)
                return;

            if (hasFirstRead)
            {
                foreach (var kv in fkMap)
                    this.reader.EnqueueForeign(owner, reader[kv.Key], kv.Value);

                return;
            }

            foreach (var col in table.Columns.Where(x => x.IsForeignKey || x.IsMany))
            {
                int index = reader.GetIndexOf(col.ForeignKey);
                if (index == -1)
                    continue;

                this.fkMap[index] = col;
                this.reader.EnqueueForeign(owner, reader[index], col);
            }
        }
    }
}
