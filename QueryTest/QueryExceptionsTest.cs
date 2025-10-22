using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Connection;
using Xunit.Abstractions;

namespace QueryTest
{
    public class QueryExceptionsTest(ITestOutputHelper? output) : DbMockTest(output)
    {
        [Fact]
        public void OnCreateQueryWithNull()
        {
            ConnectionCreator.Default = null;
            Assert.Throws<ArgumentNullException>(() => new Query("SomeTable"));
        }

        [Fact]
        public void OnQueryNullTable()
        {
            Assert.Throws<ArgumentNullException>(() => new Query(null, Creator));
        }

        [Fact]
        public void OnQueryNullConfig()
        {
            using var con = Creator.GetConnection();
            Assert.Throws<ArgumentNullException>(() => new Query("SomeTable", new ConnectionManager(null, con)));
        }

        [Fact]
        public void OnInvalidWhereOperation()
        {
            using var q = new Query(TestTableUtils.TABLE);

            Assert.Throws<ArgumentNullException>(() => q.Where("column", "", "value"));
            Assert.Throws<ArgumentNullException>(() => q.Where("column", null, "value"));
            Assert.Throws<InvalidOperationException>(() => q.Where("column", "None", "value"));
        }
    }
}
