using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using System;
using System.Diagnostics;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Defines a fluent API for configuring repository options.
    /// </summary>
    public interface IRepositoryOptionsBuilder
    {
        /// <summary>
        /// Gets the model configurator for setting up entity mappings.
        /// </summary>
        IModelConfigurator Models { get; }

        /// <summary>
        /// Configures whether to force a single database connection for all operations.
        /// </summary>
        /// <param name="value">True to force a single connection; otherwise, false.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        IRepositoryOptionsBuilder ForceSingleConnection(bool value);

        /// <summary>
        /// Sets the connection creator for database connections.
        /// </summary>
        /// <param name="connection">The connection creator to use.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        IRepositoryOptionsBuilder SetConnectionCreator(ConnectionCreator connection);

        /// <summary>
        /// Configures whether changes should be automatically committed.
        /// </summary>
        /// <param name="value">True to enable auto-commit; otherwise, false.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        IRepositoryOptionsBuilder SetAutoCommit(bool value);

        /// <summary>
        /// Sets the command timeout for database operations.
        /// </summary>
        /// <param name="timeout">The timeout value in seconds.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        IRepositoryOptionsBuilder SetCommandTimeout(int timeout);

        /// <summary>
        /// Sets the translation registry for custom type conversions.
        /// </summary>
        /// <param name="translation">The translation registry to use.</param>
        /// <returns>The current builder instance for method chaining.</returns>
        IRepositoryOptionsBuilder SetTranslation(TranslationRegistry translation);
    }

    /// <summary>
    /// Internal implementation of the repository options builder pattern.
    /// Provides a fluent API for configuring repository options.
    /// </summary>
    internal class RepositoryOptionsBuilder : IRepositoryOptionsBuilder
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly RepositoryOptions _options = new RepositoryOptions();

        public IModelConfigurator Models { get; } = new ModelConfigurator();

        public RepositoryOptionsBuilder(ConnectionCreator connection, bool forceSingleConnection)
        {
            _options.ForceSingleConnection = forceSingleConnection;
            _options.ConnectionCreator = connection;

            _options.AutoCommit = connection?.AutoCommit ?? false;
            _options.CommandTimeout = connection?.Config?.CommandTimeout ?? 30;
            _options.Translation = connection?.Config?.Translation?.Clone();
        }

        public IRepositoryOptionsBuilder ForceSingleConnection(bool value)
        {
            _options.ForceSingleConnection = value;
            return this;
        }

        public IRepositoryOptionsBuilder SetConnectionCreator(ConnectionCreator connection)
        {
            _options.ConnectionCreator = connection ?? throw new ArgumentNullException(nameof(connection));
            return this;
        }

        public IRepositoryOptionsBuilder SetAutoCommit(bool value)
        {
            _options.AutoCommit = value;
            return this;
        }

        public IRepositoryOptionsBuilder SetCommandTimeout(int timeout)
        {
            _options.CommandTimeout = timeout;
            return this;
        }

        public IRepositoryOptionsBuilder SetTranslation(TranslationRegistry translation)
        {
            _options.Translation = translation;
            return this;
        }

        public RepositoryOptions Build()
        {
            if (_options.Translation == null)
                throw new InvalidOperationException();

            if (_options.ConnectionCreator == null)
                throw new InvalidOperationException();

            ((ModelConfigurator)Models).Configure(_options.Translation);
            return _options;
        }
    }
}
