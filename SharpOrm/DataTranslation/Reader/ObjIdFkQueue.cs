using SharpOrm.Builder;
using SharpOrm.ForeignKey;
using System;
using System.Linq;
using System.Reflection;

namespace SharpOrm.DataTranslation.Reader
{
    [Obsolete]
    internal class ObjIdFkQueue : IFkQueue
    {
        public void EnqueueForeign(object owner, TranslationRegistry translator, object fkValue, IForeignKeyNode node)
        {
            if (fkValue is DBNull || fkValue is null)
                return;

            node.ColumnInfo.SetRaw(owner, MakeObjWithId(translator, node.ColumnInfo, fkValue));
        }

        public static object MakeObjWithId(TranslationRegistry translator, ColumnInfo column, object fkValue)
        {
            var fkTable = translator.GetTable(column.Type);
            var constructors = column.Type.GetConstructors();
            if (constructors.FirstOrDefault(x => x.GetParameters().Length == 0) is ConstructorInfo constructor)
            {
                object value = constructor.Invoke(new object[0]);
                fkTable.Columns.FirstOrDefault(c => c.Key)?.Set(value, fkValue);
                return value;
            }

            var valueType = fkValue?.GetType();
            if (constructors.FirstOrDefault(x => IsExpectedParameters(x.GetParameters(), valueType)) is ConstructorInfo valueConstructor)
                return valueConstructor.Invoke(new object[] { fkValue });

            return ReflectionUtils.GetDefault(column.Type);
        }

        private static bool IsExpectedParameters(ParameterInfo[] parameters, Type valueType)
        {
            return parameters.Length == 1 && parameters[0].ParameterType == valueType;
        }
    }
}
