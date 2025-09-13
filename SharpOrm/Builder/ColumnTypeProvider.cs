using SharpOrm.Builder.Grammars.Table;
using System;
using System.Data;

namespace SharpOrm.Builder
{
    public class ColumnTypeProvider : ItemsProvider<IColumnTypeMap>, IColumnTypeProvider
    {
        public void Add(Type type, string sqlType)
        {
            TryAdd(type, () => new ColumnType(type, sqlType));
        }

        public string BuildType(DataColumn column)
        {
            return Get(column)?.Build(column);
        }

        public IColumnTypeMap Get(DataColumn column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));

            return Get(column.DataType);
        }
    }
}
