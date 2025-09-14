using SharpOrm.Builder;

namespace SharpOrm.Connection
{
    public interface IConnectionCreatorFactory
    {
        ConnectionCreator Create();
    }
}
