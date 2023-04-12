using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm
{
    public interface ICell
    {
        string Name { get; }
        object Value { get; }
    }
}