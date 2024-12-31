using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Represents a dynamically mapped object from a database reader.
    /// </summary>
    public class MappedDynamic : IMappedObject
    {
        private readonly List<DbColumnReader> columns = new List<DbColumnReader>();

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
                columns.Add(new DbColumnReader(registry, reader.GetName(i), reader.GetFieldType(i)));
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
            var dObject = new ExpandoObject();

            for (int i = 0; i < columns.Count; i++)
                this.columns[i].ReadTo(dObject, reader, i);

            return dObject;
        }

        private class DbColumnReader
        {
            private readonly ISqlTranslation translation;
            private readonly string name;
            private readonly Type type;

            public DbColumnReader(TranslationRegistry registry, string name, Type type)
            {
                this.type = TranslationRegistry.GetValidTypeFor(type);
                this.translation = registry.GetFor(type);
                this.name = name;
            }

            public void ReadTo(IDictionary<string, object> target, IDataReader reader, int index)
            {
                target[this.name] = this.Read(reader, index);
            }

            public object Read(IDataReader reader, int index)
            {
                if (translation == null)
                    return reader[index];

                return translation.FromSqlValue(reader[index], type);
            }
        }
    }
}
