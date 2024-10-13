using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.Errors;

namespace QueryTest
{
    public class QueryExceptionsTest() : DbGrammarTestBase(new SqlServerQueryConfig())
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
            Assert.Throws<ArgumentNullException>(() => new Query(null, connection.Creator));
        }

        [Fact]
        public void OnQueryNullConfig()
        {
            using var con = connection.Creator.GetConnection();
            Assert.Throws<ArgumentNullException>(() => new Query("SomeTable", new ConnectionManager(null, con)));
        }

        [Fact]
        public void OnInvalidWhereOperation()
        {
            using var q = new Query(TestTableUtils.TABLE);

            Assert.Throws<ArgumentNullException>(() => q.Where("column", "", "value"));
            Assert.Throws<ArgumentNullException>(() => q.Where("column", null, "value"));
            Assert.Throws<DatabaseException>(() => q.Where("column", "None", "value"));
        }
    }
}
