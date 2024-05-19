using System;

namespace SharpOrm
{
    internal interface IDisposableWithEvent : IDisposable
    {
        event EventHandler Disposed;
    }
}
