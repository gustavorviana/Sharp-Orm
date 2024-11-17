using Xunit.Abstractions;

namespace BaseTest.Utils
{
    public class TestBase(ITestOutputHelper? output) : IDisposable
    {
        protected ITestOutputHelper Output => output ?? throw new NotImplementedException();
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