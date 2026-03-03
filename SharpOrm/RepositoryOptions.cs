using SharpOrm.Connection;
using SharpOrm.DataTranslation;

namespace SharpOrm
{
    /// <summary>
    /// Defines the configuration options for a repository.
    /// </summary>
    public interface IRepositoryOptions
    {
        /// <summary>
        /// Gets a value indicating whether to force a single database connection for all operations.
        /// </summary>
        bool ForceSingleConnection { get; }

        /// <summary>
        /// Gets a value indicating whether changes should be automatically committed.
        /// </summary>
        bool AutoCommit { get; }

        /// <summary>
        /// Gets the command timeout for database operations in seconds.
        /// </summary>
        int CommandTimeout { get; }

        /// <summary>
        /// Gets the translation registry for custom type conversions.
        /// </summary>
        TranslationRegistry Translation { get; }

        /// <summary>
        /// Gets the connection creator for database connections.
        /// </summary>
        ConnectionCreator ConnectionCreator { get; }

        /// <summary>
        /// Gets the connection management strategy used for database connections.
        /// </summary>
        ConnectionManagement ConnectionManagement { get; }
    }

    internal class RepositoryOptions : IRepositoryOptions
    {
        public bool ForceSingleConnection { get; set; }

        public bool AutoCommit { get; set; }

        public int CommandTimeout { get; set; }

        public TranslationRegistry Translation { get; set; }

        public ConnectionCreator ConnectionCreator { get; set; }

        public ConnectionManagement ConnectionManagement { get; set; }
    }
}
