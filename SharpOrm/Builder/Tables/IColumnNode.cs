using System.Collections.Generic;

namespace SharpOrm.Builder.Tables
{
    public interface IColumnNode
    {
        IReadOnlyList<IColumnNode> Children { get; }
        ColumnInfo Column { get; }
    }
}