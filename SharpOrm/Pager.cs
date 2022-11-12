using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm
{
    public class Pager<T> : IReadOnlyList<T> where T : new()
    {
        protected readonly Query query;
        private T[] items = new T[0];

        private int currentPage;
        private int peerPage;
        private int pagesQtd = 0;
        private long total = 0;

        public int CurrentPage => this.currentPage;
        public int Pages => this.pagesQtd;
        public long Total => this.total;

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

        public T this[int index] => items[index];

        public int Count => items.Length;

        public IEnumerator<T> GetEnumerator() => this.items.AsEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

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

        private void RefreshPageCount()
        {
            this.query.Offset = null;
            this.query.Limit = null;
            this.total = this.query.Count();
            this.pagesQtd = PageCalculator.CalcPages(this.total, this.peerPage);
        }

        public void Refresh()
        {
            this.RefreshPageCount();
            this.RefreshItems();
        }

        private void RefreshItems()
        {
            this.query.Offset = this.peerPage * (this.CurrentPage - 1);
            this.query.Limit = this.peerPage;

            this.items = this.GetItems().ToArray();
        }

        protected virtual IEnumerable<T> GetItems()
        {
            return this.query.All<T>();
        }
    }
}