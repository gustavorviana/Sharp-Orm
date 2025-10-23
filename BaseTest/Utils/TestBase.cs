using Xunit;
using Xunit.Abstractions;

namespace BaseTest.Utils
{
    public class TestBase(ITestOutputHelper? output) : IAsyncLifetime
    {
        protected ITestOutputHelper Output => output ?? throw new NotImplementedException();

        #region IAsyncLifetime

        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}