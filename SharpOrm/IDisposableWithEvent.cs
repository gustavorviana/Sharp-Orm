using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm
{
    internal interface IDisposableWithEvent : IDisposable
    {
        event EventHandler Disposed;
    }
}
