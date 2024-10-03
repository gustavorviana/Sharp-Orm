using Xunit.Abstractions;

namespace SharpOrm.QueryTest.Utils
{
    public class TestBase(ITestOutputHelper output) : IDisposable
    {
        protected ITestOutputHelper Output => output;
        private bool disposedValue;

        #region IDisposable

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;

            disposedValue = true;
        }

        #endregion
    }
}