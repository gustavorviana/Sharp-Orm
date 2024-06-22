using SharpOrm.DataTranslation;
using SharpOrm.DataTranslation.Reader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

namespace SharpOrm.Collections
{
    /// <summary>
    /// Provides an enumerable collection for reading data from a <see cref="DbDataReader"/>.
    /// </summary>
    /// <typeparam name="T">The type of the objects to enumerate.</typeparam>
    public class DataReaderEnumerable<T> : IEnumerable<T>
    {
        private readonly DbDataReader reader;
        private readonly IMappedObject map;
        /// <summary>
        /// Gets or sets the cancellation token.
        /// </summary>
        public CancellationToken Token { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataReaderEnumerable{T}"/> class.
        /// </summary>
        /// <param name="reader">The database reader.</param>
        /// <param name="registry">The translation registry.</param>
        /// <param name="enqueueable">The foreign key queue. If null, a default queue is used.</param>
        public DataReaderEnumerable(DbDataReader reader, TranslationRegistry registry, IFkQueue enqueueable = null)
        {
            this.map = MappedObject.Create(reader, typeof(T), enqueueable, registry);
            this.reader = reader;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataReaderEnumerable{T}"/> class.
        /// </summary>
        /// <param name="reader">The database reader.</param>
        /// <param name="map">The mapped object.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="reader"/> or <paramref name="map"/> is null.</exception>
        public DataReaderEnumerable(DbDataReader reader, IMappedObject map)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.map = map ?? throw new ArgumentNullException(nameof(map));
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<T> GetEnumerator() => new DbObjectEnumerator<T>(this.reader, this.map, this.Token);

        IEnumerator IEnumerable.GetEnumerator() => new DbObjectEnumerator(this.reader, this.map, this.Token);
    }
}
