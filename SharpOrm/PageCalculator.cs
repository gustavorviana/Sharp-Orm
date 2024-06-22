using System;

namespace SharpOrm
{
    /// <summary>
    /// Represents a page calculator that calculates page-related information based on size and peer page values.
    /// Note: Page index is 1-based.
    /// </summary>
    public class PageCalculator
    {
        /// <summary>
        /// Gets the total size.
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public long Pages { get; }

        /// <summary>
        /// Gets the number of items per page.
        /// </summary>
        public long PeerPage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageCalculator"/> class with the specified size and peer page values.
        /// </summary>
        /// <param name="size">The total size.</param>
        /// <param name="peerPage">The number of items per page.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the size or peerPage is less than or equal to zero.</exception>
        public PageCalculator(long size, long peerPage)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            if (peerPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(peerPage));

            this.PeerPage = peerPage;
            this.Size = size;

            this.Pages = CalcPages(size, peerPage);
        }

        /// <summary>
        /// Gets the size of the specified page.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <returns>The size of the specified page.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the page number is less than 1 or greater than the total number of pages.</exception>
        public long GetSize(int page)
        {
            this.CheckPage(page);

            if (page != this.Pages)
                return this.PeerPage;

            return this.Size - this.CountTo(page - 1);
        }

        /// <summary>
        /// Gets the start index of the specified page.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <returns>The start index of the specified page.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the page number is less than 1 or greater than the total number of pages.</exception>
        public long GetStartIndex(int page)
        {
            this.CheckPage(page);

            if (page == 1)
                return 0;

            if (page < this.Pages)
                return this.CountTo(page - 1);

            return (int)(this.Size - this.GetSize(page));
        }

        private long CountTo(int page)
        {
            return this.PeerPage * page;
        }

        private void CheckPage(int page)
        {
            if (page < 1 || page > this.Pages)
                throw new ArgumentOutOfRangeException(nameof(page));
        }

        /// <summary>
        /// Calculates the total number of pages based on the specified size and peer page values.
        /// </summary>
        /// <param name="size">The total size.</param>
        /// <param name="peerPage">The number of items per page.</param>
        /// <returns>The total number of pages.</returns>
        public static int CalcPages(long size, long peerPage)
        {
            return (int)Math.Ceiling(size / (double)peerPage);
        }
    }
}