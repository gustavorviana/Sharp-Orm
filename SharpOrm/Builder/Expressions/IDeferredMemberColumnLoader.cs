using SharpOrm.ForeignKey;

namespace SharpOrm.Builder.Expressions
{
    internal interface IDeferredMemberColumnLoader
    {
        IReadonlyQueryInfo Info { get; }
        string ColumnName { get; }
        IForeignKeyNode GetNode();

        string GetParentPrefix();

        bool NeedPrefix();
    }
}
