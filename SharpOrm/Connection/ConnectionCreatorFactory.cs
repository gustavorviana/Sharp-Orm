using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Connection
{
    public class ConnectionCreatorFactory : IConnectionCreatorFactory
    {
        private readonly IConnectionConfigurator _configurator;
        private readonly bool _multipleConnections;
        private readonly string _connectionString;
        private readonly Type _connectorType;
        private readonly QueryConfig _config;

        public ConnectionCreatorFactory(Type connectorType,
            QueryConfig config,
            string connectionString,
            bool multipleConnections,
            IConnectionConfigurator configurator = null)
        {
            _config = config;
            _configurator = configurator;
            _connectorType = connectorType;
            _connectionString = connectionString;
            _multipleConnections = multipleConnections;
        }

        public ConnectionCreator Create()
        {
            if (_multipleConnections)
                return new MultipleConnectionCreator(_connectorType, _config, _connectionString);

            return new SingleConnectionCreator(_connectorType, _config, _connectionString);
        }
    }
}
