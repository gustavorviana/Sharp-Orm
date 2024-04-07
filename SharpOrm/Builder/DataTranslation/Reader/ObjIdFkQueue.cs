using System;
using System.Linq;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    internal class ObjIdFkQueue : IFkQueue
    {
        public void EnqueueForeign(object owner, object fkValue, ColumnInfo column)
        {
            if (fkValue is DBNull || fkValue is null)
                return;

            column.SetRaw(owner, MakeObjWithId(column, fkValue));
        }

        public static object MakeObjWithId(ColumnInfo column, object fkValue)
        {
            var fkTable = new TableInfo(column.Type);
            object value = fkTable.CreateInstance();
            fkTable.Columns.FirstOrDefault(c => c.Key)?.Set(value, fkValue);
            return value;
        }
    }
}
