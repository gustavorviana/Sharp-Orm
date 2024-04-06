using SharpOrm.Builder.DataTranslation.Reader;
using System;
using System.Collections;
using System.Data.Common;
using System.Threading;

namespace SharpOrm.Collections
{
    internal class DbObjectEnumerator : IEnumerator, IDisposable
    {
        private readonly CancellationToken token;
        private readonly DbDataReader reader;
        private readonly IMappedObject map;

        public DbObjectEnumerator(DbDataReader reader, IMappedObject map, CancellationToken token)
        {
            this.reader = reader;
            this.token = token;
            this.map = map;
        }

        private object current;
        public object Current => this.current;

        public bool MoveNext()
        {
            bool next = this.reader.Read();
            try
            {
                this.token.ThrowIfCancellationRequested();
            }
            catch (Exception)
            {
                this.current = null;
                throw;
            }
            this.current = next ? this.map.Read(reader) : null;
            return next;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose() => this.reader.Dispose();
    }
}