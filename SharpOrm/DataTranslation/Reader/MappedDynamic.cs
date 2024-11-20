using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Represents a dynamically mapped object from a database reader.
    /// </summary>
    public class MappedDynamic : IMappedObject
    {
        private readonly List<ObjConverter> converters = new List<ObjConverter>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MappedDynamic"/> class.
        /// </summary>
        /// <param name="reader">The database reader.</param>
        /// <param name="registry">The translation registry. If null, the default registry is used.</param>
        public MappedDynamic(IDataReader reader, TranslationRegistry registry = null)
        {
            if (registry == null)
                registry = TranslationRegistry.Default;

            for (int i = 0; i < reader.FieldCount; i++)
                converters.Add(new ObjConverter(registry, reader.GetFieldType(i)));
        }

        /// <summary>
        /// Reads data from the database reader and maps it to a dynamic object.
        /// </summary>
        /// <param name="reader">The database reader.</param>
        /// <param name="registry">The translation registry. If null, the default registry is used.</param>
        /// <returns>A dynamic object containing the mapped data.</returns>
        public static dynamic Read(IDataReader reader, TranslationRegistry registry = null)
        {
            return new MappedDynamic(reader, registry).Read(reader);
        }

        /// <summary>
        /// Reads data from the database reader and maps it to a dynamic object.
        /// </summary>
        /// <param name="reader">The database reader.</param>
        /// <returns>A dynamic object containing the mapped data.</returns>
        public dynamic Read(IDataReader reader)
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

            public object Parse(IDataReader reader, int index)
            {
                if (translation == null)
                    return reader[index];

                return translation.FromSqlValue(reader[index], type);
            }
        }
    }
}
