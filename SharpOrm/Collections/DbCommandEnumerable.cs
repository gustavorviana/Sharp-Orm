using SharpOrm.Builder;
using SharpOrm.Connection;
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
    /// Provides an enumerable collection for executing a database command and reading the results.
    /// </summary>
    /// <typeparam name="T">The type of the objects to enumerate.</typeparam>
    public class DbCommandEnumerable<T> : IEnumerable<T>
    {
        private readonly ConnectionManagement _management;
        private readonly TranslationRegistry _translation;
        internal NestedMode mode = NestedMode.Attribute;
        private readonly CancellationToken _token;
        private readonly DbCommand _command;
        internal IFkQueue _fkQueue;
        private bool _hasFirstRun;

        /// <summary>
        /// Gets or sets a value indicating whether to dispose the command after execution.
        /// </summary>
        public bool DisposeCommand { get; set; } = true;

        internal ConnectionManager manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbCommandEnumerable{T}"/> class.
        /// </summary>
        /// <param name="command">The database command to execute.</param>
        /// <param name="translation">The translation registry.</param>
        /// <param name="management">The connection management strategy.</param>
        /// <param name="token">The cancellation token.</param>
        public DbCommandEnumerable(DbCommand command, TranslationRegistry translation, ConnectionManagement management = ConnectionManagement.LeaveOpen, CancellationToken token = default)
        {
            _translation = translation;
            _management = management;
            _command = command;
            _token = token;

            _command.SetCancellationToken(token);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            CheckRun();
            var reader = _command.ExecuteReader();
            return RegisterDispose(new DbObjectEnumerator<T>(reader, CreateMappedObj(reader), _token) { manager = manager });
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            CheckRun();
            var reader = _command.ExecuteReader();
            return RegisterDispose(new DbObjectEnumerator(reader, CreateMappedObj(reader), _token) { manager = manager });
        }

        private void CheckRun()
        {
            if (_hasFirstRun)
                throw new InvalidOperationException("IEnumerable can be executed only once.");

            _hasFirstRun = true;
            _command.Connection.OpenIfNeeded();
        }

        private IMappedObject CreateMappedObj(DbDataReader reader)
        {
            return MappedObject.Create(reader, typeof(T), mode, _fkQueue, _translation);
        }

        private K RegisterDispose<K>(K instance) where K : DbObjectEnumerator
        {
            instance.Disposed += (sender, e) =>
            {
                try
                {
                    if (CanClose()) _command.Connection.Close();
                }
                catch
                { }

                if (DisposeCommand)
                    try { _command.Dispose(); } catch { }
            };

            return instance;
        }

        private bool CanClose()
        {
            return _command.Transaction == null && _command.Connection.IsOpen() && (_management == ConnectionManagement.CloseOnEndOperation || _management == ConnectionManagement.CloseOnDispose);
        }
    }
}
