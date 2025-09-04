using System;
using System.Collections;
using System.Data;

namespace SharpOrm.DataTranslation.Mappers
{
    /// <summary>
    /// Provides a base implementation for mapping data from an <see cref="IDataReader"/> to objects.
    /// Maintains a reference to the <see cref="IDataReader"/> and a <see cref="TranslationRegistry"/>
    /// for type conversion, and enforces a contract for reading mapped objects.
    /// </summary>
    public abstract class BaseRecordReader : IEnumerator, IEnumerable
    {
        /// <summary>
        /// Gets the <see cref="IDataReader"/> used to read database records.
        /// </summary>
        protected IDataReader Reader { get; }

        /// <summary>
        /// Gets the <see cref="TranslationRegistry"/> used for translating database values to CLR types.
        /// </summary>
        protected TranslationRegistry Registry { get; }

        public object Current { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRecordReader"/> class with the specified data reader and translation registry.
        /// </summary>
        /// <param name="reader">The <see cref="IDataReader"/> containing the records to map.</param>
        /// <param name="registry">The <see cref="TranslationRegistry"/> used for value translation.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="reader"/> or <paramref name="registry"/> is null.
        /// </exception>
        public BaseRecordReader(IDataReader reader, TranslationRegistry registry)
        {

            Reader = reader ?? throw new ArgumentNullException(nameof(reader));
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public bool MoveNext()
        {
            if (!Reader.Read())
            {
                Current = null;
                return false;
            }

            Current = OnRead();
            return true;
        }

        /// <summary>
        /// Reads the current record from the <see cref="Reader"/> and maps it to an object.
        /// Concrete implementations must provide the logic to create the mapped object.
        /// </summary>
        /// <returns>An object representing the mapped data of the current record.</returns>
        protected abstract object OnRead();

        void IEnumerator.Reset()
            => throw new NotSupportedException("Reset is not supported because the underlying IDataReader cannot be rewound.");

        IEnumerator IEnumerable.GetEnumerator() => this;
    }
}
