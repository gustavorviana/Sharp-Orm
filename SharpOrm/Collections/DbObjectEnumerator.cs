using SharpOrm.Connection;
using SharpOrm.DataTranslation.Reader;
using SharpOrm.Msg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
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
        public DbObjectEnumerator(ConnectionManager manager, IDataReader reader, IEnumerator enumeratorBase) : base(manager, reader, enumeratorBase)
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
        private readonly IDataReader _reader;
        private readonly ConnectionManager _manager;
        private readonly IEnumerator _enumerator;

        private bool _disposed;

        /// <summary>
        /// Occurs when the enumerator is disposed.
        /// </summary>
        public event EventHandler Disposed;

        public CancellationToken Token { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbObjectEnumerator"/> class.
        /// </summary>
        /// <param name="reader">The database reader.</param>
        /// <param name="map">The mapped object.</param>
        /// <param name="token">The cancellation token.</param>
        /// <exception cref="InvalidOperationException">Thrown when the <paramref name="reader"/> is closed.</exception>
        public DbObjectEnumerator(ConnectionManager manager, IDataReader reader, IEnumerator enumeratorBase)
        {
            _enumerator = enumeratorBase;
            _manager = manager;
            _reader = reader;
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        public object Current => _enumerator.Current;

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>True if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            try
            {
                return _enumerator.MoveNext();
            }
            catch (Exception ex)
            {
                Token.ThrowIfCancellationRequested();
                _manager?.SignalException(ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown always since this method is not implemented.</exception>
        public void Reset() => _enumerator.Reset();

        #region IDisposable

        ~DbObjectEnumerator()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
                return;

            _disposed = true;
            _reader.Dispose();
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    [Obsolete]
    internal class ObsoleteEnumerator : IEnumerator
    {
        private readonly IDataReader _reader;

        private readonly IMappedObject _map;

        public CancellationToken Token { get; set; }

        public object Current { get; private set; }

        public ObsoleteEnumerator(IDataReader reader, IMappedObject map)
        {
            _reader = reader;
            _map = map;
        }

        public bool MoveNext()
        {
            // Check for cancellation or end of data
            if (Token.IsCancellationRequested || !_reader.Read())
            {
                Current = null;
                return false;
            }

            // Convert the current database record to a strongly-typed object
            Current = _map.Read(_reader);
            return true;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}