using SharpOrm.Builder.DataTranslation.Reader;
using System;
using System.Collections;
using System.Data.Common;
using System.Threading;

namespace SharpOrm.Collections
{
    internal class DbObjectEnumerator : IEnumerator, IDisposable
    {
        public CancellationToken Token { get; }
        private readonly DbDataReader reader;
        private readonly IMappedObject map;
        public bool Disposed { get; private set; }

        public DbObjectEnumerator(DbDataReader reader, IMappedObject map, CancellationToken token)
        {
            this.reader = reader;
            this.Token = token;
            this.map = map;
        }

        private object current;
        public object Current => this.current;

        public bool MoveNext()
        {
            bool next = this.reader.Read();
            try
            {
                this.Token.ThrowIfCancellationRequested();
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

        public void Dispose()
        {
            if (this.Disposed)
                return;

            this.Disposed = true;
            this.reader.Dispose();
        }
    }
}