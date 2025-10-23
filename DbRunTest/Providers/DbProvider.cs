using DbRunTest.DbInitializer;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Collections.Concurrent;
using System.Data.Common;

namespace DbRunTest.Providers
{
    public abstract class DbProvider<TQueryConfig, TDbType> : DbProvider
    {
        protected DbProvider() : base(typeof(TQueryConfig), typeof(TDbType))
        {
        }
    }

    public abstract class DbProvider(Type queryConfigType, Type connectorType) : IAsyncDisposable
    {
        private static readonly ConcurrentDictionary<Type, DbProvider> _providers = [];
        private readonly ConcurrentDictionary<bool, QueryConfig> _config = [];
        protected abstract DbInicializer? Initializer { get; }
        private bool _disposed;

        public bool Builded { get; private set; }
        public Type ConnectorType => connectorType;
        public string? ConnectionString { get; private set; }

        public async Task BuildAsync()
        {
            if (Builded)
                return;

            ConnectionString = await OnBuildAsync();
            await InitializeDb();
            Builded = true;
        }

        protected abstract Task<string> OnBuildAsync();

        private async Task InitializeDb()
        {
            if (Initializer == null)
                return;

            using var creator = GetConnectionCreator(false, false);
            using var manager = new ConnectionManager(creator);
            await Initializer.InitAsync(manager);
        }

        public QueryConfig GetConfig(bool safe = true)
        {
            return _config.GetOrAdd(safe,
                filter => (QueryConfig)Activator.CreateInstance(queryConfigType, filter ? null : new object[] { false })!
            );
        }

        public static void Register(DbProvider provider)
        {
            _providers.TryAdd(provider.ConnectorType, provider);
        }

        public static DbProvider Get<TConnection>() where TConnection : DbConnection, new()
            => Get(typeof(TConnection));

        public static DbProvider Get(Type type)
            => _providers.GetValueOrDefault(type)
               ?? throw new NotSupportedException($"Connector for type '{type.Name}' has not been registered.");

        public ConnectionCreator GetConnectionCreator(bool multiple, bool safe)
        {
            if (multiple)
                return new MultipleConnectionCreator(ConnectorType, GetConfig(safe), ConnectionString);

            return new SingleConnectionCreator(ConnectorType, GetConfig(safe), ConnectionString);
        }

        #region IAsyncDisposable
        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (_disposed)
                return;

            if (Initializer != null)
            {
                using var creator = GetConnectionCreator(false, false);
                using var manager = new ConnectionManager(creator);
                await Initializer.ResetAsync(manager);
            }

            _disposed = true;

            return;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
