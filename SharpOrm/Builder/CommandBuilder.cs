using SharpOrm.Builder.Grammars;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace SharpOrm.Builder
{
    internal class CommandBuilder : IDisposable
    {
        private bool disposed;

        private readonly bool leaveOpen = false;
        private readonly ConnectionManager manager;
        private readonly CancellationToken token;
        private readonly DbCommand command;
        private DbDataReader reader;

        public int Timeout
        {
            get => command.CommandTimeout;
            set => command.CommandTimeout = value;
        }

        public bool LogQuery { get; set; }

        internal CommandBuilder(ConnectionManager manager, bool leaveOpen, CancellationToken token) : this(manager, token)
        {
            this.leaveOpen = leaveOpen;
        }

        public CommandBuilder(ConnectionManager manager, CancellationToken token)
        {
            this.manager = manager;

            command = manager.Connection.CreateCommand().SetCancellationToken(token);
            command.Transaction = manager.Transaction;
        }

        /// <summary>
        /// Configure an SQL expression.
        /// </summary>
        /// <param name="expression">The SQL expression to configure.</param>
        /// <returns>The number of affected rows, excluding the last expression in the collection.</returns>
        public int ConfigureExpression(SqlExpression expression)
        {
            if (expression is SqlExpressionCollection expCollection)
                return ConfigureLot(expCollection);

            SetExpression(expression);
            return 0;
        }

        /// <summary>
        /// Configures and executes a batch of SQL expressions.
        /// </summary>
        /// <param name="collection">The collection of SQL expressions to be executed.</param>
        /// <returns>The number of affected rows, excluding the last expression in the collection.</returns>
        public int ConfigureLot(SqlExpressionCollection collection)
        {
            int total = collection.Expressions.Length;
            if (total == 0)
                return 0;

            OpenIfNeeded();

            int result = 0;
            for (int i = 0; i < total - 1; i++)
                result += command.SetExpression(collection.Expressions[i]).ExecuteNonQuery();

            SetExpression(collection.Expressions[total - 1]);

            return result;
        }

        /// <summary>
        /// Sets the SQL expression for the command.
        /// </summary>
        /// <param name="expression">The SQL expression to set.</param>
        /// <returns>The current instance of <see cref="CommandBuilder"/>.</returns>
        public CommandBuilder SetExpression(SqlExpression expression)
        {
            Log(expression);
            command.SetExpression(expression);
            return this;
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns the number of rows affected.
        /// </summary>
        /// <returns>The number of rows affected by the SQL query.</returns>
        public int ExecuteNonQuery()
        {
            return OpenIfNeeded().ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns the result as an enumerable collection of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the returned collection.</typeparam>
        /// <param name="registry">Optional. The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> representing the query results.</returns>
        public DbCommandEnumerable<T> ExecuteEnumerable<T>(TranslationRegistry registry = null, bool disposeCommand = false)
        {
            return new DbCommandEnumerable<T>(OpenIfNeeded(), registry, manager.Management, token)
            {
                DisposeCommand = disposeCommand,
                manager = manager,
                mode = manager.Config.NestedMapMode
            };
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        public T ExecuteScalar<T>(TranslationRegistry registry = null)
        {
            OpenIfNeeded();
            return (registry ?? TranslationRegistry.Default).FromSql<T>(command.ExecuteScalar());
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the result set.</returns>
        public object ExecuteScalar(TranslationRegistry registry = null)
        {
            OpenIfNeeded();

            try
            {
                return (registry ?? TranslationRegistry.Default).FromSql(command.ExecuteScalar());
            }
            catch (Exception ex)
            {
                token.ThrowIfCancellationRequested();
                manager.SignalException(ex);
                throw;
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Executes the query and returns a <see cref="DbDataReader"/> to read the results.
        /// </summary>
        /// <param name="behavior">The behavior of the command execution.</param>
        /// <returns>A <see cref="DbDataReader"/> to read the results.</returns>
        public DbDataReader GetReader(CommandBehavior behavior = CommandBehavior.Default)
        {
            if (reader != null)
                SafeCancel();

            return reader = OpenIfNeeded().ExecuteReader(behavior);
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other columns are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>An array of the first column of all rows in the result set.</returns>
        public T[] ExecuteArrayScalar<T>(TranslationRegistry translationRegistry = null)
        {
            try
            {
                return DbCommandExtension.ExecuteArrayScalar<T>(OpenIfNeeded(), translationRegistry, manager.Management, token).ToArray();
            }
            catch (Exception ex)
            {
                manager.SignalException(ex);
                throw;
            }
        }

        /// <summary>
        /// Executes the query and returns the number of records affected.
        /// </summary>
        /// <returns>The number of records affected by the query.</returns>
        public int ExecuteAndRecordsAffected()
        {
            using (var reader = OpenIfNeeded().ExecuteReader())
                return reader.RecordsAffected;
        }

        private void SafeCancel()
        {
            try { command.Cancel(); } catch { }
        }

        private void Log(SqlExpression expression)
        {
            if (LogQuery)
                Grammar.QueryLogger?.Invoke(expression.ToString());
        }

        private DbCommand OpenIfNeeded()
        {
            manager.Connection.OpenIfNeeded();
            return command;
        }

        #region IDisposabe
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (!leaveOpen)
                SafeCancel();
            if (disposing)
            {
                try { reader?.Dispose(); } catch { }
                if (!leaveOpen)
                    try { command.Dispose(); } catch { }
            }

            reader = null;
            disposed = true;
        }

        ~CommandBuilder()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
