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
        private readonly ConnectionManagement management;
        private readonly TranslationRegistry translation;
        private readonly CancellationToken token;
        private readonly DbCommand command;
        internal IFkQueue fkQueue;
        private bool hasFirstRun;

        /// <summary>
        /// Gets or sets a value indicating whether to dispose the command after execution.
        /// </summary>
        public bool DisposeCommand { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbCommandEnumerable{T}"/> class.
        /// </summary>
        /// <param name="command">The database command to execute.</param>
        /// <param name="translation">The translation registry.</param>
        /// <param name="management">The connection management strategy.</param>
        /// <param name="token">The cancellation token.</param>
        public DbCommandEnumerable(DbCommand command, TranslationRegistry translation, ConnectionManagement management = ConnectionManagement.LeaveOpen, CancellationToken token = default)
        {
            this.translation = translation;
            this.management = management;
            this.command = command;
            this.token = token;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            this.CheckRun();
            var reader = command.ExecuteReader();
            return RegisterDispose(new DbObjectEnumerator<T>(reader, this.CreateMappedObj(reader), token));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            this.CheckRun();
            var reader = command.ExecuteReader();
            return RegisterDispose(new DbObjectEnumerator(reader, this.CreateMappedObj(reader), token));
        }

        private void CheckRun()
        {
            if (this.hasFirstRun)
                throw new InvalidOperationException("IEnumerable can be executed only once.");

            this.hasFirstRun = true;
            command.Connection.OpenIfNeeded();
        }

        private IMappedObject CreateMappedObj(DbDataReader reader)
        {
            return MappedObject.Create(reader, typeof(T), this.fkQueue, translation);
        }

        private K RegisterDispose<K>(K instance) where K : DbObjectEnumerator
        {
            if (this.management != ConnectionManagement.CloseOnEndOperation)
            {
                if (this.DisposeCommand)
                    try { this.command.Dispose(); } catch { }

                return instance;
            }

            instance.Disposed += (sender, e) =>
            {
                try
                {
                    if (this.command.Transaction == null && this.command.Connection.IsOpen())
                        this.command.Connection.Close();
                }
                catch
                { }

                if (this.DisposeCommand)
                    try { this.command.Dispose(); } catch { }
            };

            return instance;
        }
    }
}
