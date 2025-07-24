using SharpOrm.Builder;

namespace SharpOrm.ForeignKey
{
    public interface IForeignKeyNode
    {
        TableInfo TableInfo { get; }
        DbName Name { get; }
        QueryInfo RootInfo { get; }
        ColumnInfo ColumnInfo { get; }
    }
}
