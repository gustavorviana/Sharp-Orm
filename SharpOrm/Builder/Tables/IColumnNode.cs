namespace SharpOrm.Builder.Tables
{
    public interface IColumnNode : IWithColumnNode
    {
        ColumnInfo Column { get; }
        bool IsCollection { get; }
    }
}