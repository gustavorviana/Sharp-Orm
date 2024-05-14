using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SharpOrm.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(WeakRef_DebugView<>))]
    internal class WeakComponentsRef<T> : IReadOnlyCollection<T>, IDisposable where T : Component
    {
        private readonly List<WeakReference> refs = new List<WeakReference>();
        private bool disposed;

        public int Count => refs.Count;
        internal int AliveCount => refs.Count(x => x.IsAlive);

        public T this[int index] => GetValue(this.refs[index]);

        public void Add(T item)
        {
            lock (this)
            {
                if (this.Count <= 10)
                    this.RemoveNotAlive();

                item.Disposed += OnItemDisposed;
                refs.Add(new WeakReference(item));
            }
        }

        private void OnItemDisposed(object sender, EventArgs e)
        {
            if (!(sender is T obj))
                return;

            obj.Disposed -= OnItemDisposed;
            this.Remove(obj);
        }

        public void Clear()
        {
            lock (this)
                refs.Clear();
        }

        public void RemoveNotAlive()
        {
            for (int i = this.refs.Count - 1; i >= 0; i--)
                if (!this.refs[i].IsAlive)
                    this.refs.RemoveAt(i);
        }

        public bool Remove(T item)
        {
            lock (this)
            {
                int index = this.IndexOf(item);
                if (index < 0)
                    return false;

                this.refs.RemoveAt(index);
                return true;
            }
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < this.refs.Count; i++)
                if (GetValue(this.refs[i])?.Equals(item) ?? false)
                    return i;

            return -1;
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public IEnumerator<T> GetEnumerator() => this.refs.Select(GetValue).GetEnumerator();

        private static T GetValue(WeakReference reference)
        {
            return reference.IsAlive ? reference.Target as T : null;
        }

        #region IDisposable

        ~WeakComponentsRef()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            if (this.disposed) return;

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                for (int i = this.Count - 1; i >= 0; i--)
                    if (this.refs[i].IsAlive)
                        this.SafeDispose(this[i]);

            this.refs.Clear();

            disposed = true;
        }

        private void SafeDispose(IDisposable disposable)
        {
            try
            {
                disposable?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{typeof(T).FullName}.Dispose failed: " + ex.Message);
            }
        }

        #endregion
    }

    internal sealed class WeakRef_DebugView<T> where T : Component
    {
        private readonly WeakComponentsRef<T> collection;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => collection.ToArray();

        public WeakRef_DebugView(WeakComponentsRef<T> collection)
        {
            this.collection = collection;
        }
    }
}
