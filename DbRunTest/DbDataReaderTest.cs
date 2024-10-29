using BaseTest.Utils;
using SharpOrm;

namespace DbRunTest
{
    public class DbDataReaderTest() : DbMockTest()
    {
        [Fact]
        public void CancelRead()
        {
            Connection.QueryReaders.Add("SELECT * FROM [table]", () => new BaseTest.Mock.MockDataReader(new Cell("Id", 1)) { ReadDelay = 20 });
            CancellationTokenSource src = new(10);

            using var query = new Query("table", this.Manager) { Token = src.Token };
            using var reader = query.ExecuteReader();

            Assert.False(reader.Read(), "The command was not canceled.");
        }

        [Fact]
        public void ThrowReadCancelledToken()
        {
            Connection.QueryReaders.Add("SELECT * FROM [table]", () => new BaseTest.Mock.MockDataReader(new Cell("Id", 1)));
            CancellationTokenSource src = new();
            src.Cancel();

            using var query = new Query("table", this.Manager) { Token = src.Token };
            Assert.Throws<OperationCanceledException>(query.ExecuteReader);
        }
    }
}
