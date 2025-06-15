using SharpOrm.DataTranslation;

namespace SharpOrm.ForeignKey
{
    internal interface INodeCreationListener
    {
        void Created(ForeignKeyNode node);
    }
}
