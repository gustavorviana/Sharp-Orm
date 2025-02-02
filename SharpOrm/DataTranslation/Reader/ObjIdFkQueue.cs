using SharpOrm.Builder;
using System;
using System.Linq;

namespace SharpOrm.DataTranslation.Reader
{
    internal class ObjIdFkQueue : IFkQueue
    {
        public void EnqueueForeign(object owner, TranslationRegistry translator, object fkValue, ColumnInfo column)
        {
            if (fkValue is DBNull || fkValue is null)
                return;

            column.SetRaw(owner, MakeObjWithId(translator, column, fkValue));
        }

        public static object MakeObjWithId(TranslationRegistry translator, ColumnInfo column, object fkValue)
        {
            var fkTable = translator.GetTable(column.Type);
            object value = Activator.CreateInstance(column.Type);
            fkTable.Columns.FirstOrDefault(c => c.Key)?.Set(value, fkValue);
            return value;
        }
    }
}
