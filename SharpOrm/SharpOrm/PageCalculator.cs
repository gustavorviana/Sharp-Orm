using System;

namespace SharpOrm
{
    public class PageCalculator
    {
        public long Size { get; }
        public long Pages { get; }
        public long PeerPage { get; }

        public PageCalculator(long size, long peerPage)
        {
            this.PeerPage = peerPage;
            this.Size = size;

            this.Pages = CalcPages(size, peerPage);
        }

        public long GetSize(int page)
        {
            this.CheckPage(page);

            if (page != this.Pages)
                return this.PeerPage;

            return this.Size - this.CountTo(page - 1);
        }

        public long GetStartIndex(int page)
        {
            this.CheckPage(page);

            if (page == 1)
                return 0;

            if (page < this.Pages)
                return this.CountTo(page - 1);

            return (int)(this.Size - this.GetSize(page));
        }

        /// <summary>
        /// Contar os items da página 1 até a página inserida
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        private long CountTo(int page)
        {
            return this.PeerPage * page;
        }

        private void CheckPage(int page)
        {
            if (page <= 0 || page > this.Pages)
                throw new ArgumentOutOfRangeException(nameof(page));
        }

        public static int CalcPages(long size, long peerPage)
        {
            return (int)Math.Ceiling(size / (double)peerPage);
        }
    }
}
