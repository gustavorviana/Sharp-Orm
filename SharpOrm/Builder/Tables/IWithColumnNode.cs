using System.Collections.Generic;

namespace SharpOrm.Builder.Tables
{
    public interface IWithColumnNode
    {
        IReadOnlyList<IColumnNode> Nodes { get; }
    }
}
