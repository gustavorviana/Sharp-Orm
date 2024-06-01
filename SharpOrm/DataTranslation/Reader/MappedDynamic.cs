using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using SharpOrm.DataTranslation;

namespace SharpOrm.DataTranslation.Reader
{
    public class MappedDynamic : IMappedObject
    {
        private readonly List<ObjConverter> converters = new List<ObjConverter>();

        public MappedDynamic(DbDataReader reader, TranslationRegistry registry = null)
        {
            if (registry == null)
                registry = TranslationRegistry.Default;

            for (int i = 0; i < reader.FieldCount; i++)
                converters.Add(new ObjConverter(registry, reader.GetFieldType(i)));
        }

        public static dynamic Read(DbDataReader reader, TranslationRegistry registry = null)
        {
            return new MappedDynamic(reader, registry).Read(reader);
        }

        public dynamic Read(DbDataReader reader)
        {
            var dObject = (IDictionary<string, object>)new ExpandoObject();

            for (int i = 0; i < reader.FieldCount; i++)
                dObject[reader.GetName(i)] = converters[i].Parse(reader, i);

            return dObject;
        }

        private class ObjConverter
        {
            private readonly ISqlTranslation translation;
            private readonly Type type;

            public ObjConverter(TranslationRegistry registry, Type type)
            {
                this.type = TranslationRegistry.GetValidTypeFor(type);
                translation = registry.GetFor(type);
            }

            public object Parse(DbDataReader reader, int index)
            {
                if (translation == null)
                    return reader[index];

                return translation.FromSqlValue(reader[index], type);
            }
        }
    }
}
