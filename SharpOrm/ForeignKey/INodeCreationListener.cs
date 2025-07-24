using SharpOrm.Builder;
using SharpOrm.DataTranslation;

namespace SharpOrm.ForeignKey
{
    internal interface INodeCreationListener : IWithQueryInfo
    {
        void Created(ForeignKeyNode node);
    }
}
