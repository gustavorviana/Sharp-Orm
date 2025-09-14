using System.Data.Common;

namespace SharpOrm.Connection
{
    public interface IConnectionConfigurator
    {
        void Configure(DbConnection connection);
    }
}
