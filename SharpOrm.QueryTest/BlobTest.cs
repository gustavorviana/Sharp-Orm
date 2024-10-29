using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.DataTranslation;
using Xunit.Abstractions;

namespace QueryTest
{
    public class BlobTest(ITestOutputHelper? output) : DbMockTest(output)
    {
        private readonly byte[] bytes = [1, 2, 3, 4, 5, 6, 7, 8, 9];

        [Fact]
        public void InsertBytesTest()
        {
            using var query = new Query(Consts.Files.TABLE);

            var grammar = query.Grammar().Insert([new Cell(Consts.Files.BINARY, bytes)]);
            QueryAssert.EqualDecoded("INSERT INTO [Files] ([bin]) VALUES (@p1); SELECT SCOPE_IDENTITY();", [bytes], grammar);
        }

        [Fact]
        public void InsertStreamTest()
        {
            using var query = new Query(Consts.Files.TABLE);

            var grammar = query.Grammar().Insert([new Cell(Consts.Files.BINARY, bytes)]);
            QueryAssert.EqualDecoded("INSERT INTO [Files] ([bin]) VALUES (@p1); SELECT SCOPE_IDENTITY();", [bytes], grammar);
        }

        [Fact]
        public void ToSqlBytesTest()
        {
            var registry = TranslationRegistry.Default;

            Assert.Equal(bytes, registry.ToSql(bytes, typeof(byte[])));
            Assert.Equal(bytes, registry.ToSql(new MemoryStream(bytes), typeof(byte[])));
        }

        [Fact]
        public void FromSqlBytesToStreamTest()
        {
            var registry = TranslationRegistry.Default;

            AssertStream(registry.FromSql(bytes, typeof(MemoryStream)));
            AssertStream(registry.FromSql(new MemoryStream(bytes), typeof(MemoryStream)));
        }

        private void AssertStream(object obj)
        {
            Assert.IsType<MemoryStream>(obj);
            Assert.Equal(bytes, ((MemoryStream)obj).ToArray());
        }
    }
}
