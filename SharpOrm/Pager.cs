using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm
{
    public class Pager<T> : IReadOnlyList<T>, IDisposable where T : new()
    {
        #region Fields
        protected readonly Query query;
        private T[] items = new T[0];

        private int currentPage;
        private int peerPage;
        private int pagesQtd = 0;
        private long total = 0;
        private bool disposed;
        #endregion

        #region Properties
        public int CurrentPage => this.currentPage;
        public int Pages => this.pagesQtd;
        public long Total => this.total;

        public int Count => items.Length;
        public T this[int index] => items[index];
        #endregion

        protected Pager(Query query, int peerPage, int page = 1)
        {
            this.query = query;
            this.peerPage = peerPage;
            this.currentPage = page;
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

            int lastPage = this.currentPage;
            this.currentPage = page;

            try
            {
                this.Refresh();
            }
            catch (Exception)
            {
                this.currentPage = lastPage;
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
                this.currentPage = this.pagesQtd;

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
            this.total = this.query.Count();
            this.pagesQtd = PageCalculator.CalcPages(this.total, this.peerPage);
        }

        private void RefreshItems()
        {
            this.query.Offset = this.peerPage * (this.CurrentPage - 1);
            this.query.Limit = this.peerPage;

            this.items = this.GetItems().ToArray();
        }

        protected virtual IEnumerable<T> GetItems()
        {
            return this.query.ReadResults<T>();
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
            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            if (this.disposed)
                throw new ObjectDisposedException(GetType().FullName);

            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}