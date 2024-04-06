using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    public class MappedDynamic : IMappedObject
    {
        private readonly List<ObjConverter> converters = new List<ObjConverter>();

        public MappedDynamic(TranslationRegistry registry, DbDataReader reader)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                this.converters.Add(new ObjConverter(registry, reader.GetFieldType(i)));
        }

        public object Read(DbDataReader reader)
        {
            var dObject = (IDictionary<string, object>)new ExpandoObject();

            for (int i = 0; i < reader.FieldCount; i++)
                dObject[reader.GetName(i)] = this.converters[i].Parse(reader, i);

            return dObject;
        }

        private class ObjConverter
        {
            private readonly ISqlTranslation translation;
            private readonly Type type;

            public ObjConverter(TranslationRegistry registry, Type type)
            {
                this.type = TranslationRegistry.GetValidTypeFor(type);
                this.translation = registry.GetFor(type);
            }

            public object Parse(DbDataReader reader, int index)
            {
                if (this.translation == null)
                    return reader[index];

                return this.translation.FromSqlValue(reader[index], this.type);
            }
        }
    }
}
