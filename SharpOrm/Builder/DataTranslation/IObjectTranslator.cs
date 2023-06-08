using System;
using System.Data.Common;

namespace SharpOrm.Builder.DataTranslation
{
    public interface IObjectTranslator
    {
        TranslationConfig Config { get; }
        T ParseFromReader<T>(DbDataReader reader) where T : new();

        Row ToRow(object obj, Type type);

        string GetTableNameOf(Type type);

        TableInfo GetLoader(Type type);
    }
}
