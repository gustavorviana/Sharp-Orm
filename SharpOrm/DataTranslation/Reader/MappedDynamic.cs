using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Represents a dynamically mapped object from a database record.
    /// </summary>
    public class MappedDynamic : IMappedObject
    {
        private readonly List<DbColumnReader> columns = new List<DbColumnReader>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MappedDynamic"/> class.
        /// </summary>
        /// <param name="record">The database record.</param>
        /// <param name="registry">The translation registry. If null, the default registry is used.</param>
        public MappedDynamic(IDataRecord record, TranslationRegistry registry = null)
        {
            if (registry == null)
                registry = TranslationRegistry.Default;

            for (int i = 0; i < record.FieldCount; i++)
                columns.Add(new DbColumnReader(registry, record.GetName(i), record.GetFieldType(i)));
        }

        /// <summary>
        /// Reads data from the database record and maps it to a dynamic object.
        /// </summary>
        /// <param name="record">The database record.</param>
        /// <param name="registry">The translation registry. If null, the default registry is used.</param>
        /// <returns>A dynamic object containing the mapped data.</returns>
        public static dynamic Read(IDataRecord record, TranslationRegistry registry = null)
        {
            return new MappedDynamic(record, registry).Read(record);
        }

        /// <summary>
        /// Reads data from the database record and maps it to a dynamic object.
        /// </summary>
        /// <param name="record">The database record.</param>
        /// <returns>A dynamic object containing the mapped data.</returns>
        public dynamic Read(IDataRecord record)
        {
            var dObject = new ExpandoObject();

            for (int i = 0; i < columns.Count; i++)
                this.columns[i].ReadTo(dObject, record, i);

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

            public void ReadTo(IDictionary<string, object> target, IDataRecord record, int index)
            {
                target[this.name] = this.Read(record, index);
            }

            public object Read(IDataRecord record, int index)
            {
                if (translation == null)
                    return record[index];

                return translation.FromSqlValue(record[index], type);
            }
        }
    }
}
