using SharpOrm.DataTranslation.Reader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

namespace SharpOrm.Collections
{
    public class DbObjectEnumerator<T> : DbObjectEnumerator, IEnumerator<T>
    {
        public DbObjectEnumerator(DbDataReader reader, IMappedObject map, CancellationToken token) : base(reader, map, token)
        {
        }

        public new T Current => (T)base.Current;
    }

    public class DbObjectEnumerator : IEnumerator, IDisposable
    {
        public CancellationToken Token { get; }
        private readonly DbDataReader reader;
        private readonly IMappedObject map;
        public event EventHandler Disposed;
        private bool _disposed;

        public DbObjectEnumerator(DbDataReader reader, IMappedObject map, CancellationToken token)
        {
            if (reader.IsClosed)
                throw new InvalidOperationException($"It is not possible to use a closed {nameof(DbDataReader)}.");

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

        #region IDisposable

        ~DbObjectEnumerator()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
                return;

            this._disposed = true;
            this.reader.Dispose();
            this.Disposed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}