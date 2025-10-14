using SharpOrm.DataTranslation;
using System;

namespace SharpOrm.Builder.Tables.Loaders
{
    internal interface ITableInfo
    {
        Type Type { get; }
        TranslationRegistry Registry { get; }
        string Name { get; }
        SoftDeleteAttribute SoftDelete { get; }
        HasTimestampAttribute Timestamp { get; }
        IColumnLoader ColumnLoader { get; }
    }
}
