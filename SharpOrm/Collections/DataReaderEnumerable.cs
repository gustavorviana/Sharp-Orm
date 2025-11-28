using SharpOrm.DataTranslation;
using SharpOrm.DataTranslation.Reader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace SharpOrm.Collections
{
    /// <summary>
    /// Provides an enumerable wrapper around a database reader that converts database records to strongly-typed objects.
    /// Supports cancellation through CancellationToken and handles both new and obsolete mapping approaches.
    /// </summary>
    /// <typeparam name="T">The type of objects to enumerate from the database reader.</typeparam>
    public class DataReaderEnumerable<T> : IEnumerable<T>
    {
        /// <summary>
        /// Internal enumerator that handles the actual iteration over database records.
        /// Can be either a BaseRecordReader (new approach) or ObsoleteEnumerator (deprecated approach).
        /// </summary>
        private readonly IEnumerator _enumerator;

        /// <summary>
        /// Gets or sets the cancellation token used to cancel enumeration operations.
        /// </summary>
        public CancellationToken Token { get; set; }

        /// <summary>
        /// Initializes a new instance using the modern factory pattern approach.
        /// This is the preferred constructor for new implementations.
        /// </summary>
        /// <param name="reader">The database reader containing the data to enumerate.</param>
        /// <param name="registry">Registry containing translation mappings for converting database values to object properties.</param>
        /// <param name="factory">Factory responsible for creating the appropriate record reader for type T.</param>
        public DataReaderEnumerable(IDataReader reader, TranslationRegistry registry, IRecordReaderFactory factory)
        {
            _enumerator = factory.OfType(typeof(T), reader, registry);
        }

        /// <summary>
        /// Initializes a new instance using the legacy mapping approach with optional foreign key support.
        /// </summary>
        /// <param name="reader">The database reader containing the data to enumerate.</param>
        /// <param name="registry">Registry containing translation mappings for converting database values to object properties.</param>
        /// <param name="enqueueable">Optional foreign key queue for handling related data loading. Uses default queue if null.</param>
        [Obsolete("IMappedObject is deprecated and will be removed in version 4.0. Use BaseRecordReader instead.")]
        public DataReaderEnumerable(DbDataReader reader, TranslationRegistry registry, IFkQueue enqueueable = null)
        {
            _enumerator = new ObsoleteEnumerator(reader, MappedObject.Create(reader, typeof(T), enqueueable, registry));
        }

        /// <summary>
        /// Initializes a new instance using a pre-configured mapped object.
        /// </summary>
        /// <param name="reader">The database reader containing the data to enumerate.</param>
        /// <param name="map">Pre-configured mapping object that defines how to convert database records to type T.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="reader"/> or <paramref name="map"/> is null.</exception>
        [Obsolete("IMappedObject is deprecated and will be removed in version 4.0. Use BaseRecordReader instead.")]
        public DataReaderEnumerable(DbDataReader reader, IMappedObject map)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _enumerator = new ObsoleteEnumerator(reader, map);
        }

        /// <summary>
        /// Returns a strongly-typed enumerator that iterates through the collection.
        /// Wraps the internal enumerator to provide type-safe iteration.
        /// </summary>
        /// <returns>An enumerator that can iterate through the collection of type T objects.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (_enumerator is ObsoleteEnumerator obsolete)
                obsolete.Token = Token;

            if (_enumerator is BaseRecordReader reader)
                reader.Token = Token;

            return new TEnumerator(_enumerator);
        }

        /// <summary>
        /// Returns the internal enumerator and configures it with the current cancellation token.
        /// This method handles both obsolete and modern enumerator types.
        /// </summary>
        /// <returns>An enumerator that can iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            // Configure cancellation token for obsolete enumerator
            if (_enumerator is ObsoleteEnumerator obsolete)
                obsolete.Token = Token;

            // Configure cancellation token for modern record reader
            if (_enumerator is BaseRecordReader reader)
                reader.Token = Token;

            return _enumerator;
        }

        private class TEnumerator : IEnumerator<T>
        {
            private readonly IEnumerator _enumerator;

            public TEnumerator(IEnumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public T Current => (T)_enumerator.Current;

            object IEnumerator.Current => _enumerator.Current;

            public void Dispose() { }

            public bool MoveNext() => _enumerator.MoveNext();

            public void Reset() => _enumerator.Reset();
        }
    }
}