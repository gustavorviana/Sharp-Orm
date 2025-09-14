using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Maps the columns of an <see cref="IDataRecord"/> to a dynamic object (<see cref="ExpandoObject"/>).
    /// Each column is associated with a translation from a <see cref="TranslationRegistry"/>, 
    /// allowing flexible conversion from database types to CLR types. 
    /// 
    /// Implements <see cref="BaseRecordReader"/> to provide a standardized way to map database records
    /// to dynamic objects, especially useful when the target structure is not known at compile time.
    /// </summary>
    internal class DynamicRecordReader : BaseRecordReader
    {
        private readonly List<DbColumn> _columns = new List<DbColumn>();

        public new dynamic Current => base.Current;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicRecordReader"/> class.
        /// Prepares internal column mappings based on the given <see cref="IDataRecord"/>.
        /// </summary>
        /// <param name="record">The database record containing column names and types.</param>
        /// <param name="registry">
        /// The translation registry to handle type conversions. 
        /// If null, the default registry (<see cref="TranslationRegistry.Default"/>) is used.
        /// </param>
        public DynamicRecordReader(IDataReader reader, TranslationRegistry registry) : base(reader, registry)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                _columns.Add(new DbColumn(registry, reader.GetName(i), reader.GetFieldType(i)));
        }

        protected override object OnRead()
        {
            var dObject = new ExpandoObject();

            for (int i = 0; i < _columns.Count; i++)
                _columns[i].ReadTo(dObject, Reader, i);

            return dObject;
        }

        private class DbColumn
        {
            private readonly ISqlTranslation _translation;
            private readonly string _name;
            private readonly Type _type;

            public DbColumn(TranslationRegistry registry, string name, Type type)
            {
                _type = TranslationRegistry.GetValidTypeFor(type);
                _translation = registry.GetFor(type);
                _name = name;
            }

            public void ReadTo(IDictionary<string, object> target, IDataRecord record, int index)
            {
                target[_name] = Read(record, index);
            }

            public object Read(IDataRecord record, int index)
            {
                if (_translation == null)
                    return record[index];

                return _translation.FromSqlValue(record[index], _type);
            }
        }
    }
}
