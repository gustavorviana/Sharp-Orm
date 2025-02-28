using SharpOrm.Builder.Grammars;
using SharpOrm.Collections;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm.Builder
{
    internal class CommandBuilder : IDisposable
    {
        private bool disposed;

        private readonly bool leaveOpen = false;
        private readonly TranslationRegistry registry;
        private readonly ConnectionManager manager;
        private readonly CancellationToken token;
        private readonly DbCommand command;
        private DbDataReader reader;

        /// <summary>
        /// Gets or sets the command timeout.
        /// </summary>
        public int Timeout
        {
            get => command.CommandTimeout;
            set => command.CommandTimeout = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to log the query.
        /// </summary>
        public bool LogQuery { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBuilder"/> class with the specified manager, registry, and leaveOpen flag.
        /// </summary>
        /// <param name="manager">The connection manager.</param>
        /// <param name="registry">The translation registry.</param>
        /// <param name="leaveOpen">Indicates whether to leave the connection open.</param>
        internal CommandBuilder(ConnectionManager manager, TranslationRegistry registry, bool leaveOpen) : this(manager, registry)
        {
            this.leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBuilder"/> class with the specified manager.
        /// </summary>
        /// <param name="manager">The connection manager.</param>
        internal CommandBuilder(ConnectionManager manager) : this(manager, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBuilder"/> class with the specified manager and registry.
        /// </summary>
        /// <param name="manager">The connection manager.</param>
        /// <param name="registry">The translation registry.</param>
        public CommandBuilder(ConnectionManager manager, TranslationRegistry registry)
        {
            this.manager = manager;
            this.registry = registry ?? TranslationRegistry.Default;

            command = manager.Connection.CreateCommand();
            command.Transaction = manager.Transaction;
        }

        /// <summary>
        /// Sets the cancellation token for the command.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The current instance of <see cref="CommandBuilder"/>.</returns>
        public CommandBuilder SetCancellationToken(CancellationToken token)
        {
            command.SetCancellationToken(token);
            return this;
        }

        /// <summary>
        /// Asynchronously sets the SQL expression and returns the number of affected rows.
        /// </summary>
        /// <param name="expression">The SQL expression to set.</param>
        /// <returns>A task representing the asynchronous operation, with the number of affected rows.</returns>
        public Task<int> SetExpressionWithAffectedRowsAsync(SqlExpression expression)
        {
            return TaskUtils.Async(() => SetExpressionWithAffectedRows(expression));
        }

        /// <summary>
        /// Configure an SQL expression.
        /// </summary>
        /// <param name="expression">The SQL expression to configure.</param>
        /// <returns>The number of affected rows, excluding the last expression in the collection.</returns>
        public int SetExpressionWithAffectedRows(SqlExpression expression)
        {
            if (expression is BatchSqlExpression expCollection)
                return SetBatchExpressionWithAffectedRows(expCollection);

            SetExpression(expression);
            return 0;
        }

        public Task<CommandBuilder> SetExpressionAsync(SqlExpression expression)
        {
            return TaskUtils.Async(() => SetExpression(expression));
        }

        /// <summary>
        /// Configure an SQL expression.
        /// </summary>
        /// <param name="expression">The SQL expression to configure.</param>
        /// <returns>The number of affected rows, excluding the last expression in the collection.</returns>
        public CommandBuilder SetExpression(SqlExpression expression)
        {
            if (expression is BatchSqlExpression expCollection)
                return SetBatchExpression(expCollection);

            InternalSetExpression(expression);
            return this;
        }

        /// <summary>
        /// Configures and executes a batch of SQL expressions.
        /// </summary>
        /// <param name="collection">The collection of SQL expressions to be executed.</param>
        /// <returns>The number of affected rows, excluding the last expression in the collection.</returns>
        private CommandBuilder SetBatchExpression(BatchSqlExpression collection)
        {
            int total = collection.Expressions.Length;
            if (total == 0)
                return this;

            OpenIfNeeded();

            for (int i = 0; i < total - 1; i++)
                command.SetExpression(collection.Expressions[i]).ExecuteNonQuery();

            SetExpression(collection.Expressions[total - 1]);
            return this;
        }

        private int SetBatchExpressionWithAffectedRows(BatchSqlExpression collection)
        {
            int total = collection.Expressions.Length;
            if (total == 0)
                return 0;

            OpenIfNeeded();

            int result = 0;
            for (int i = 0; i < total - 1; i++)
            {
                InternalSetExpression(collection.Expressions[i]);
                result += InternalExecuteWithRecordsAffected(CommandBehavior.Default);
            }

            InternalSetExpression(collection.Expressions[total - 1]);

            return result;
        }

        public CommandBuilder SetExpression(string query, params object[] args)
        {
            return InternalSetExpression(new SqlExpression(query, args));
        }

        /// <summary>
        /// Sets the SQL expression for the command.
        /// </summary>
        /// <param name="expression">The SQL expression to set.</param>
        /// <returns>The current instance of <see cref="CommandBuilder"/>.</returns>
        private CommandBuilder InternalSetExpression(SqlExpression expression)
        {
            Log(expression);
            command.SetExpression(expression);
            return this;
        }

        public async Task<int> ExecuteNonQueryAsync()
        {
            try
            {
                return (await OpenIfNeededAsync()).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                manager.SignalException(ex);
                throw;
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns the number of rows affected.
        /// </summary>
        /// <returns>The number of rows affected by the SQL query.</returns>
        public int ExecuteNonQuery()
        {
            try
            {
                return OpenIfNeeded().ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                manager.SignalException(ex);
                throw;
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Executes a SQL statement against a connection object and returns the result as an enumerable collection of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the returned collection.</typeparam>
        /// <param name="registry">Optional. The <see cref="TranslationRegistry"/> used for mapping query results, if provided.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> representing the query results.</returns>
        public DbCommandEnumerable<T> ExecuteEnumerable<T>(bool disposeCommand = false)
        {
            return new DbCommandEnumerable<T>(OpenIfNeeded(), registry, manager.Management, token)
            {
                DisposeCommand = disposeCommand,
                manager = manager,
                mode = manager.Config.NestedMapMode
            };
        }

        public async Task<T> ExecuteScalarAsync<T>()
        {
            try
            {
                await OpenIfNeededAsync();
                return registry.FromSql<T>(command.ExecuteScalar());
            }
            catch (Exception ex)
            {
                manager.SignalException(ex);
                throw;
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>The first column of the first row in the result set.</returns>
        public T ExecuteScalar<T>()
        {
            try
            {
                OpenIfNeeded();
                return registry.FromSql<T>(command.ExecuteScalar());
            }
            catch (Exception ex)
            {
                manager.SignalException(ex);
                throw;
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        public async Task<object> ExecuteScalarAsync()
        {
            await OpenIfNeededAsync();

            try
            {
                return registry.FromSql(command.ExecuteScalar());
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
        /// Executes the query and returns the first column of the first row in the result set. All other columns and rows are ignored.
        /// </summary>
        /// <returns>The first column of the first row in the result set.</returns>
        public object ExecuteScalar()
        {
            OpenIfNeeded();

            try
            {
                return registry.FromSql(command.ExecuteScalar());
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
        public DbDataReader ExecuteReader(CommandBehavior behavior = CommandBehavior.Default)
        {
            if (reader != null)
                SafeCancel();

            try
            {
                return reader = OpenIfNeeded().ExecuteReader(behavior);
            }
            catch (Exception ex)
            {
                manager.SignalException(ex);
                throw;
            }
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other columns are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>An array of the first column of all rows in the result set.</returns>
        public async Task<T[]> ExecuteArrayScalarAsync<T>()
        {
            try
            {
                return await DbCommandExtension.ExecuteArrayScalarAsync<T>(await OpenIfNeededAsync(), registry, manager.Management, token);
            }
            catch (Exception ex)
            {
                manager.SignalException(ex);
                throw;
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Executes the query and returns the first column of all rows in the result. All other columns are ignored.
        /// </summary>
        /// <typeparam name="T">Type to which the returned value should be converted.</typeparam>
        /// <returns>An array of the first column of all rows in the result set.</returns>
        public T[] ExecuteArrayScalar<T>()
        {
            try
            {
                return DbCommandExtension.ExecuteArrayScalar<T>(OpenIfNeeded(), registry, manager.Management, token);
            }
            catch (Exception ex)
            {
                manager.SignalException(ex);
                throw;
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Executes the query and returns the number of records affected.
        /// </summary>
        /// <returns>The number of records affected by the query.</returns>
        public int ExecuteWithRecordsAffected(CommandBehavior behavior = CommandBehavior.Default)
        {
            OpenIfNeeded();
            return InternalExecuteWithRecordsAffected(behavior);
        }

        /// <summary>
        /// Executes the query and returns the number of records affected asynchronously with the specified command behavior.
        /// </summary>
        /// <param name="behavior">The behavior of the command execution.</param>
        /// <returns>A task representing the asynchronous operation, with the number of records affected.</returns>
        public async Task<int> ExecuteWithRecordsAffectedAsync(CommandBehavior behavior = CommandBehavior.Default)
        {
            await OpenIfNeededAsync();
            return InternalExecuteWithRecordsAffected(behavior);
        }

        private int InternalExecuteWithRecordsAffected(CommandBehavior behavior)
        {
            try
            {
                using (var reader = command.ExecuteReader(behavior))
                    return reader.RecordsAffected;
            }
            catch (Exception ex)
            {
                manager.SignalException(ex);
                throw;
            }
            finally
            {
                manager.CloseByEndOperation();
            }
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

        private async Task<DbCommand> OpenIfNeededAsync()
        {
            await manager.Connection.OpenIfNeededAsync();
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
