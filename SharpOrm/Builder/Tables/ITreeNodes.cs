using System.Collections.Generic;

namespace SharpOrm.Builder.Tables
{
    internal interface ITreeNodes<T>
    {
        List<T> Nodes { get; }
    }
}
