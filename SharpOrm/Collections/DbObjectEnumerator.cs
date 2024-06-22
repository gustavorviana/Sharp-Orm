using SharpOrm.DataTranslation.Reader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

namespace SharpOrm.Collections
{
    /// <summary>
    /// Enumerator for reading database objects of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the objects to enumerate.</typeparam>
    internal class DbObjectEnumerator<T> : DbObjectEnumerator, IEnumerator<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbObjectEnumerator{T}"/> class.
        /// </summary>
        /// <param name="reader">The database reader.</param>
        /// <param name="map">The mapped object.</param>
        /// <param name="token">The cancellation token.</param>
        public DbObjectEnumerator(DbDataReader reader, IMappedObject map, CancellationToken token) : base(reader, map, token)
        {
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        public new T Current => (T)base.Current;
    }

    /// <summary>
    /// Enumerator for reading database objects.
    /// </summary>
    internal class DbObjectEnumerator : IEnumerator, IDisposable
    {
        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        public CancellationToken Token { get; }

        private readonly DbDataReader reader;
        private readonly IMappedObject map;
        private bool _disposed;

        /// <summary>
        /// Occurs when the enumerator is disposed.
        /// </summary>
        public event EventHandler Disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbObjectEnumerator"/> class.
        /// </summary>
        /// <param name="reader">The database reader.</param>
        /// <param name="map">The mapped object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <exception cref="InvalidOperationException">Thrown when the <paramref name="reader"/> is closed.</exception>
        public DbObjectEnumerator(DbDataReader reader, IMappedObject map, CancellationToken token)
        {
            if (reader.IsClosed)
                throw new InvalidOperationException($"It is not possible to use a closed {nameof(DbDataReader)}.");

            this.reader = reader;
            this.Token = token;
            this.map = map;
        }

        private object current;
        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        public object Current => this.current;

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>True if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
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

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown always since this method is not implemented.</exception>
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