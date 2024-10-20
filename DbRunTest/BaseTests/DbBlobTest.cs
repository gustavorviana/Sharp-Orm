using BaseTest.Models.Blob;
using BaseTest.Utils;
using DbRunTest.Fixtures;
using SharpOrm;
using System.Data.Common;
using Xunit.Abstractions;

namespace DbRunTest.BaseTests
{
    public abstract class DbBlobTest<T>(ITestOutputHelper output, DbFixture<T> connection) : DbTestBase(output, connection), IClassFixture<DbFixture<T>> where T : DbConnection, new()
    {
        [Fact]
        public void InsertBytesTest()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            using var query = NewQuery(Consts.Files.TABLE);
            query.Insert(new Cell(Consts.Files.BINARY, bytes));

            var row = query.FirstRow();
            Assert.IsType<byte[]>(row[Consts.Files.BINARY]);
            Assert.Equal(bytes, (byte[])row[Consts.Files.BINARY]);
        }

        [Fact]
        public void ReadBytesTest()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            using var query = NewQuery<BytesModel>();
            query.Insert(new Cell(Consts.Files.BINARY, new MemoryStream(bytes)));

            var obj = query.FirstOrDefault();
            Assert.IsType<byte[]>(obj.File);
            Assert.Equal(bytes, obj.File);
        }
    }
}
