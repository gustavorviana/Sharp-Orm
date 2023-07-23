using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm
{
    public class Pager<T> : IReadOnlyList<T>, IDisposable where T : new()
    {
        #region Properties/Fields
        protected readonly Query query;
        private T[] items = new T[0];

        private int peerPage;
        private bool disposed;

        public int CurrentPage { get; private set; }
        public int Pages { get; private set; }
        public long Total { get; private set; }

        public int Count => items.Length;
        public T this[int index] => items[index];
        #endregion

        protected Pager(Query query, int peerPage, int page = 1)
        {
            this.query = query;
            this.peerPage = peerPage;
            this.CurrentPage = page;
        }

        public static Pager<T> FromBuilder(Query builder, int peerPage, int currentPage)
        {
            Pager<T> list = new Pager<T>(builder.Clone(true), peerPage, currentPage);

            list.Refresh();

            return list;
        }

        public IEnumerator<T> GetEnumerator() => this.items.AsEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="page">One based page.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void GoToPage(int page)
        {
            if (page < 1 || page > this.Pages)
                throw new ArgumentOutOfRangeException(nameof(page));

            int lastPage = this.CurrentPage;
            this.CurrentPage = page;

            try
            {
                this.Refresh();
            }
            catch (Exception)
            {
                this.CurrentPage = lastPage;
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">One based page</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetPeerPage(int value)
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value));

            this.peerPage = value;
            this.RefreshPageCount();

            if (this.CurrentPage > this.Pages)
                this.CurrentPage = this.Pages;

            this.RefreshItems();
        }

        public void Refresh()
        {
            this.RefreshPageCount();
            this.RefreshItems();
        }

        private void RefreshPageCount()
        {
            this.query.Offset = null;
            this.query.Limit = null;
            this.Total = this.query.Count();
            this.Pages = PageCalculator.CalcPages(this.Total, this.peerPage);
        }

        private void RefreshItems()
        {
            this.query.Offset = this.peerPage * (this.CurrentPage - 1);
            this.query.Limit = this.peerPage;

            this.items = this.GetItems().ToArray();
        }

        protected virtual IEnumerable<T> GetItems()
        {
            return this.query.GetEnumerable<T>();
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                this.query.Dispose();

            this.items = new T[0];

            disposed = true;
        }

        ~Pager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            if (this.disposed)
                throw new ObjectDisposedException(GetType().FullName);

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}