using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace SharpOrm.Collections
{
    /// <summary>
    /// A collection of weak references to disposable components.
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection, which must be a class and implement <see cref="IDisposable"/>.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(WeakRef_DebugView<>))]
    internal class WeakComponentsRef<T> : IReadOnlyCollection<T>, IDisposable where T : class, IDisposable
    {
        private readonly List<WeakReference> refs = new List<WeakReference>();
        private readonly object _lock = new object();
        private bool disposed;

        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        public int Count => refs.Count;
        /// <summary>
        /// Gets the number of alive elements in the collection.
        /// </summary>
        internal int AliveCount => refs.Count(x => x.IsAlive);

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index] => GetValue(this.refs[index]);

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            lock (this._lock)
            {
                if (this.Count <= 10)
                    this.InternalRemoveNotAlive();

                AddDisposedEvent(item);
                refs.Add(new WeakReference(item));
            }
        }

        private void OnItemDisposed(object sender, EventArgs e)
        {
            if (!(sender is T obj))
                return;

            RemoveDisposedEvent(obj);
            this.Remove(obj);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            this.Clear(false);
        }

        /// <summary>
        /// Removes all items from the collection and optionally disposes them.
        /// </summary>
        /// <param name="disposeItems">If true, disposes the items before removing them.</param>
        public void Clear(bool disposeItems)
        {
            lock (this._lock)
            {
                if (disposeItems)
                    for (int i = this.refs.Count - 1; i >= 0; i--)
                        if (this.refs[i].IsAlive) SafeDispose(this[i]);

                this.refs.Clear();
            }
        }

        /// <summary>
        /// Removes all items from the collection that are not alive.
        /// </summary>
        public void RemoveNotAlive()
        {
            lock (this._lock) this.InternalRemoveNotAlive();
        }

        private void InternalRemoveNotAlive()
        {
            for (int i = this.refs.Count - 1; i >= 0; i--)
                if (!this.refs[i].IsAlive)
                    this.refs.RemoveAt(i);
        }

        /// <summary>
        /// Removes the first occurrence of a specific item from the collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>True if the item is successfully removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            lock (this._lock)
            {
                int index = this.IndexOf(item);
                if (index < 0)
                    return false;

                this.refs.RemoveAt(index);
                return true;
            }
        }

        /// <summary>
        /// Determines the index of a specific item in the collection.
        /// </summary>
        /// <param name="item">The item to locate in the collection.</param>
        /// <returns>The index of the item if found in the collection; otherwise, -1.</returns>
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

        private void AddDisposedEvent(T obj)
        {
            if (obj is Component c) c.Disposed += this.OnItemDisposed;
            else if (obj is IDisposableWithEvent e) e.Disposed += this.OnItemDisposed;
        }

        private void RemoveDisposedEvent(T obj)
        {
            if (obj is Component c) c.Disposed -= this.OnItemDisposed;
            else if (obj is IDisposableWithEvent e) e.Disposed -= this.OnItemDisposed;
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

            if (disposing) this.Clear(true);
            else this.refs.Clear();

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

    internal sealed class WeakRef_DebugView<T> where T : class, IDisposable
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
