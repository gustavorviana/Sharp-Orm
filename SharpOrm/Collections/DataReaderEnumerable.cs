using SharpOrm.DataTranslation;
using SharpOrm.DataTranslation.Reader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

namespace SharpOrm.Collections
{
    public class DataReaderEnumerable<T> : IEnumerable<T>
    {
        private readonly DbDataReader reader;
        private readonly IMappedObject map;
        public CancellationToken Token { get; set; }

        public DataReaderEnumerable(DbDataReader reader, TranslationRegistry registry, IFkQueue enqueueable = null)
        {
            this.map = MappedObject.Create(reader, typeof(T), enqueueable, registry);
            this.reader = reader;
        }

        public DataReaderEnumerable(DbDataReader reader, IMappedObject map)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public IEnumerator<T> GetEnumerator() => new DbObjectEnumerator<T>(this.reader, this.map, this.Token);

        IEnumerator IEnumerable.GetEnumerator() => new DbObjectEnumerator(this.reader, this.map, this.Token);
    }
}
