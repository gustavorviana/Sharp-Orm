using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm
{
    /// <summary>
    /// Represents a pager for navigating and retrieving data in a paginated manner.
    /// </summary>
    /// <typeparam name="T">The type of items in the pager.</typeparam>
    public class Pager<T> : IReadOnlyList<T>, IDisposable
    {
        #region Properties/Fields
        private readonly Column countColunm;
        protected readonly Query query;
        private T[] items = new T[0];

        private int peerPage;
        private bool disposed;

        /// <summary>
        /// Gets the current page number.
        /// </summary>
        public int CurrentPage { get; private set; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int Pages { get; private set; }

        /// <summary>
        /// Gets the total number of items across all pages.
        /// </summary>
        public long Total { get; private set; }

        /// <summary>
        /// Gets the number of items on the current page of the pager.
        /// </summary>
        public int Count => items.Length;

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        /// <param name="index">The index of the item to get.</param>
        /// <returns>The item at the specified index.</returns>
        public T this[int index] => items[index];
        #endregion

        protected Pager(Query query, int peerPage, int page, Column countColunm)
        {
            this.query = query;
            this.CurrentPage = page;
            this.peerPage = peerPage;
            this.countColunm = countColunm;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Pager{T}"/> class using a query builder.
        /// </summary>
        /// <param name="builder">The query builder to use.</param>
        /// <param name="peerPage">The number of items per page.</param>
        /// <param name="currentPage">The current page number.</param>
        /// <param name="countColumnName">Column name used to count the number of items.</param>
        /// <returns>An instance of the <see cref="Pager{T}"/> class.</returns>
        public static Pager<T> FromBuilder(Query builder, int peerPage, int currentPage, string countColumnName)
        {
            return FromBuilder(builder, peerPage, currentPage, new Column(countColumnName));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Pager{T}"/> class using a query builder.
        /// </summary>
        /// <param name="builder">The query builder to use.</param>
        /// <param name="peerPage">The number of items per page.</param>
        /// <param name="currentPage">The current page number.</param>
        /// <param name="countColumn">Column used to count the number of items.</param>
        /// <returns>An instance of the <see cref="Pager{T}"/> class.</returns>
        public static Pager<T> FromBuilder(Query builder, int peerPage, int currentPage, Column countColumn = null)
        {
            Pager<T> list = new Pager<T>(builder.Clone(true), peerPage, currentPage, countColumn);

            list.Refresh();

            return list;
        }

        public IEnumerator<T> GetEnumerator() => this.items.AsEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

        /// <summary>
        /// Navigates to the specified page.
        /// </summary>
        /// <param name="page">The one-based page number to navigate to.</param>
        /// <remarks>
        /// This value is one-based, meaning that the page count starts from 1.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified page number is out of range.</exception>
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
        /// Sets the number of items to display per page.
        /// </summary>
        /// <param name="value">The number of items per page (one-based).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified value is less than 1.</exception>
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

        /// <summary>
        /// Refreshes the pager, updating the item collection and page count.
        /// </summary>
        public void Refresh()
        {
            this.RefreshPageCount();
            this.RefreshItems();
        }

        /// <summary>
        /// Calculates the total number of pages and updates the pager's page count.
        /// </summary>
        private void RefreshPageCount()
        {
            this.Total = this.countColunm == null ? this.query.Count() : this.query.Count(this.countColunm);
            this.Pages = PageCalculator.CalcPages(this.Total, this.peerPage);
        }

        /// <summary>
        /// Retrieves and updates the items for the current page.
        /// </summary>
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

        /// <summary>
        /// Disposes of the resources used by the pager.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources, false if finalizing.</param>
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

        /// <summary>
        /// Disposes of the pager instance, releasing resources.
        /// </summary>
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